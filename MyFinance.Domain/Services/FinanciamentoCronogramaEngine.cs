using System;
using System.Collections.Generic;
using System.Linq;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Núcleo de geração do cronograma de amortização, compartilhado pelo Price e
    /// pelo SAC. Os dois sistemas diferem em uma única coisa — qual parâmetro do
    /// contrato fica congelado: o Price congela a parcela e deixa a amortização
    /// crescer; o SAC congela a amortização e deixa a parcela cair. Todo o resto
    /// (juros sobre o saldo, pagamento extra, arredondamento, quitação) é
    /// idêntico, e duplicá-lo em dois laços separados foi o que deixou o Price
    /// calculando o total pago por fórmula fechada — algo que deixa de valer
    /// assim que o cronograma pode ser perturbado por um pagamento extra.
    /// </summary>
    internal static class FinanciamentoCronogramaEngine
    {
        /// <summary>Cronograma pronto, com os totais já acumulados linha a linha.</summary>
        /// <param name="TotalPago">Tudo que saiu do bolso no contrato: parcelas contratuais + pagamentos extras.</param>
        /// <param name="PrazoFinalMeses">Em quantos meses o contrato de fato quitou — menor que o contratado quando há amortização extra reduzindo prazo.</param>
        internal record Cronograma(
            IReadOnlyList<ParcelaFinanciamento> Parcelas,
            decimal TotalPago,
            decimal TotalJuros,
            decimal TotalAmortizacaoExtra,
            int PrazoFinalMeses,
            decimal TotalSeguros,
            decimal TotalTaxaAdministracao);

        /// <summary>
        /// Gera o cronograma completo de um financiamento.
        /// </summary>
        /// <param name="sistema">Qual parâmetro do contrato fica congelado.</param>
        /// <param name="valorFinanciado">Principal financiado (PV), em R$.</param>
        /// <param name="taxaJurosMensalPercentual">Taxa do contrato ao mês, em % (ex.: 1.5 para 1,5% a.m.).</param>
        /// <param name="numParcelas">Número de parcelas mensais contratadas.</param>
        /// <param name="amortizacaoExtra">Pagamentos além da parcela. Nulo quando o mutuário paga só o contratado.</param>
        /// <param name="encargos">Seguros e tarifas cobrados por cima da parcela. Nulo quando não informados.</param>
        internal static Cronograma Gerar(
            SistemaAmortizacao sistema,
            decimal valorFinanciado,
            decimal taxaJurosMensalPercentual,
            int numParcelas,
            AmortizacaoExtra? amortizacaoExtra = null,
            EncargosFinanciamento? encargos = null)
        {
            Validar(valorFinanciado, taxaJurosMensalPercentual, numParcelas);
            encargos?.Validar();

            var i = taxaJurosMensalPercentual / 100;
            var extraMensal = ValidarValorMensal(amortizacaoExtra);
            var extrasAvulsasPorMes = AgruparAvulsasPorMes(amortizacaoExtra?.Avulsas, numParcelas);
            var modo = amortizacaoExtra?.Modo ?? ModoAmortizacaoExtra.ReduzirPrazo;

            var parametroContratual = CalcularParametroContratual(sistema, valorFinanciado, i, numParcelas);

            var parcelas = new List<ParcelaFinanciamento>(numParcelas);
            var saldoDevedor = valorFinanciado;
            decimal totalPago = 0;
            decimal totalJuros = 0;
            decimal totalExtra = 0;
            decimal totalSeguros = 0;
            decimal totalTaxaAdministracao = 0;

            for (var numero = 1; numero <= numParcelas; numero++)
            {
                // Os encargos do mês incidem sobre o saldo de ABERTURA, antes de
                // qualquer abatimento — é o saldo que o contrato tem enquanto o
                // risco segurado existe.
                var seguroMip = encargos?.MipDoMes(saldoDevedor) ?? 0m;
                var seguroDfi = encargos?.DfiDoMes() ?? 0m;
                var taxaAdministracao = encargos?.TaxaAdministracaoMensal ?? 0m;

                var juros = Math.Round(saldoDevedor * i, 2);
                var amortizacao = AmortizacaoDoMes(sistema, parametroContratual, juros);
                var extra = extraMensal + extrasAvulsasPorMes.GetValueOrDefault(numero, 0m);

                // Liquidação: na última parcela contratual — ou antes dela, se o
                // abatimento do mês já alcançou o saldo — o que resta é quitado
                // por inteiro. Sem isso o arredondamento a centavos deixa um
                // resíduo no fim do contrato (o Price termina devendo alguns
                // centavos, o SAC sobra o que não divide exato), e um extra maior
                // que a dívida geraria saldo negativo.
                if (numero == numParcelas || amortizacao + extra >= saldoDevedor)
                {
                    // A amortização contratual vem primeiro, limitada à dívida; o
                    // extra cobre só o que ainda falta, para não cobrar do mutuário
                    // mais do que ele deve. O resíduo de arredondamento que sobrar
                    // é absorvido pela parcela contratual — não é pagamento extra,
                    // e classificá-lo como tal poluiria o total de amortização extra.
                    amortizacao = Math.Min(amortizacao, saldoDevedor);
                    extra = Math.Min(extra, saldoDevedor - amortizacao);
                    amortizacao = saldoDevedor - extra;

                    saldoDevedor = 0m;
                }
                else
                {
                    saldoDevedor = Math.Round(saldoDevedor - amortizacao - extra, 2);
                }

                var valorParcela = juros + amortizacao;

                parcelas.Add(new ParcelaFinanciamento(
                    numero, valorParcela, juros, amortizacao, saldoDevedor,
                    extra, seguroMip, seguroDfi, taxaAdministracao));

                // TotalPago é o custo do empréstimo em si (principal + juros); os
                // encargos são somados à parte, porque não são dívida — quem os
                // mistura aqui quebra a identidade "total pago = principal + juros".
                totalPago += valorParcela + extra;
                totalJuros += juros;
                totalExtra += extra;
                totalSeguros += seguroMip + seguroDfi;
                totalTaxaAdministracao += taxaAdministracao;

                if (saldoDevedor == 0m)
                    break;

                // No modo "reduzir parcela" o prazo é sagrado: o contrato é
                // reescrito sobre o saldo que sobrou, diluído nos meses que ainda
                // faltam. No modo "reduzir prazo" o parâmetro fica como está e o
                // saldo menor simplesmente acaba antes.
                if (extra > 0 && modo == ModoAmortizacaoExtra.ReduzirParcela)
                    parametroContratual = CalcularParametroContratual(sistema, saldoDevedor, i, numParcelas - numero);
            }

            return new Cronograma(
                parcelas,
                Math.Round(totalPago, 2),
                Math.Round(totalJuros, 2),
                Math.Round(totalExtra, 2),
                parcelas.Count,
                Math.Round(totalSeguros, 2),
                Math.Round(totalTaxaAdministracao, 2));
        }

        /// <summary>
        /// Validação comum aos dois sistemas. Mora aqui para que Price e SAC
        /// recusem exatamente os mesmos argumentos, com as mesmas mensagens.
        /// </summary>
        internal static void Validar(decimal valorFinanciado, decimal taxaJurosMensalPercentual, int numParcelas)
        {
            if (valorFinanciado <= 0)
                throw new ArgumentException("O valor financiado deve ser maior que zero.", nameof(valorFinanciado));

            if (taxaJurosMensalPercentual < 0)
                throw new ArgumentException("A taxa de juros mensal não pode ser negativa.", nameof(taxaJurosMensalPercentual));

            if (numParcelas <= 0)
                throw new ArgumentException("O número de parcelas deve ser maior que zero.", nameof(numParcelas));
        }

        private static decimal ValidarValorMensal(AmortizacaoExtra? amortizacaoExtra)
        {
            var valor = amortizacaoExtra?.ValorMensal ?? 0m;

            if (valor < 0)
                throw new ArgumentException(
                    "A amortização extra mensal não pode ser negativa.", nameof(amortizacaoExtra));

            return valor;
        }

        /// <summary>
        /// Indexa os pagamentos avulsos por mês, somando os que caem no mesmo mês.
        /// Um mês fora do prazo contratado é erro de entrada, não algo a ignorar
        /// em silêncio: o usuário teria um resultado que não corresponde ao que
        /// ele pediu.
        /// </summary>
        private static Dictionary<int, decimal> AgruparAvulsasPorMes(
            IReadOnlyList<AmortizacaoExtraAvulsa>? avulsas, int numParcelas)
        {
            if (avulsas is null || avulsas.Count == 0)
                return new Dictionary<int, decimal>();

            foreach (var avulsa in avulsas)
            {
                if (avulsa.Mes <= 0 || avulsa.Mes > numParcelas)
                    throw new ArgumentException(
                        $"O mês de uma amortização extra deve estar entre 1 e {numParcelas}.", nameof(avulsas));

                if (avulsa.Valor <= 0)
                    throw new ArgumentException(
                        "O valor de uma amortização extra deve ser maior que zero.", nameof(avulsas));
            }

            return avulsas
                .GroupBy(a => a.Mes)
                .ToDictionary(g => g.Key, g => g.Sum(a => a.Valor));
        }

        /// <summary>
        /// O valor que o contrato mantém fixo: a parcela, no Price; a amortização,
        /// no SAC. É a única diferença entre os dois sistemas.
        /// </summary>
        private static decimal CalcularParametroContratual(
            SistemaAmortizacao sistema, decimal saldoDevedor, decimal i, int parcelasRestantes) =>
            sistema switch
            {
                SistemaAmortizacao.Price => CalcularParcelaPrice(saldoDevedor, i, parcelasRestantes),
                SistemaAmortizacao.Sac => Math.Round(saldoDevedor / parcelasRestantes, 2),
                _ => throw new ArgumentOutOfRangeException(nameof(sistema), sistema, "Sistema de amortização desconhecido.")
            };

        /// <summary>Quanto do principal a parcela do mês abate, dado o parâmetro congelado.</summary>
        private static decimal AmortizacaoDoMes(SistemaAmortizacao sistema, decimal parametroContratual, decimal juros) =>
            sistema switch
            {
                SistemaAmortizacao.Price => Math.Round(parametroContratual - juros, 2),
                SistemaAmortizacao.Sac => parametroContratual,
                _ => throw new ArgumentOutOfRangeException(nameof(sistema), sistema, "Sistema de amortização desconhecido.")
            };

        /// <summary>
        /// Fórmula da Tabela Price: PMT = PV * [i * (1+i)^n] / [(1+i)^n - 1].
        /// Com taxa zero a parcela é o principal dividido igualmente.
        /// </summary>
        private static decimal CalcularParcelaPrice(decimal saldoDevedor, decimal i, int parcelasRestantes)
        {
            if (i == 0)
                return Math.Round(saldoDevedor / parcelasRestantes, 2);

            var fator = (decimal)Math.Pow((double)(1 + i), parcelasRestantes);
            return Math.Round(saldoDevedor * (i * fator) / (fator - 1), 2);
        }
    }
}
