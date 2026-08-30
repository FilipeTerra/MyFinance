using System;
using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Simula a fase de retirada (desacumulação) de um investimento: saques
    /// mensais fixos sobre um saldo que continua rendendo. O saque bruto que
    /// consome o saldo é sempre o mesmo independentemente do imposto — o IR
    /// só afeta quanto do saque chega líquido ao investidor, calculado
    /// proporcionalmente ao ganho embutido em cada saque (base de custo
    /// amortizada mês a mês), reaproveitando os calculadores de imposto já
    /// existentes por categoria.
    /// </summary>
    /// <remarks>
    /// Simplificação assumida: o dinheiro em retirada já é considerado de
    /// "longo prazo" (a acumulação já levou anos), então a tabela regressiva de
    /// renda fixa/previdência é sempre aplicada na alíquota mínima (piso), em
    /// vez de recalcular o prazo de cada aporte original.
    /// </remarks>
    public static class RetiradaCalculator
    {
        /// <summary>Prazo (em meses) usado só para forçar a alíquota mínima das tabelas regressivas — ver o <c>remarks</c> da classe.</summary>
        private const int PrazoParaAliquotaMinima = 999;

        public record MesRetirada(
            int Mes, decimal SaldoInicial, decimal SaqueBruto,
            decimal AliquotaImpostoPercentual, decimal ValorImposto, decimal SaqueLiquido, decimal SaldoFinal);

        public record ResultadoRetirada(bool Esgotou, int? MesEsgotamento, IReadOnlyList<MesRetirada> Evolucao);

        /// <summary>
        /// Saque mensal bruto máximo que mantém o saldo em zero (nem antes, nem
        /// depois) exatamente ao fim do prazo desejado — fórmula fechada de
        /// amortização de renda certa (parcela postecipada).
        /// </summary>
        public static decimal CalcularSaqueMaximoSustentavel(decimal saldoInicial, decimal taxaJurosAnualPercentual, int prazoMeses)
        {
            ValidarComuns(saldoInicial, taxaJurosAnualPercentual);
            if (prazoMeses <= 0)
                throw new ArgumentException("O prazo em meses deve ser maior que zero.", nameof(prazoMeses));

            var taxaMensal = TaxaMensal(taxaJurosAnualPercentual);
            if (taxaMensal == 0)
                return Math.Round(saldoInicial / prazoMeses, 2);

            var fator = Math.Pow((double)(1 + taxaMensal), prazoMeses);
            var saque = saldoInicial * (decimal)fator * taxaMensal / (decimal)(fator - 1);
            return Math.Round(saque, 2);
        }

        /// <summary>
        /// Quantos meses um saque mensal bruto fixo leva para esgotar o saldo.
        /// Retorna null quando o saque é sustentável indefinidamente (menor ou
        /// igual ao rendimento mensal do saldo, isto é, o saldo nunca encolhe).
        /// </summary>
        public static int? CalcularMesesAteEsgotar(decimal saldoInicial, decimal saqueMensal, decimal taxaJurosAnualPercentual)
        {
            ValidarComuns(saldoInicial, taxaJurosAnualPercentual);
            if (saqueMensal <= 0)
                throw new ArgumentException("O saque mensal deve ser maior que zero.", nameof(saqueMensal));

            var taxaMensal = TaxaMensal(taxaJurosAnualPercentual);

            if (taxaMensal == 0)
                return (int)Math.Ceiling(saldoInicial / saqueMensal);

            if (saqueMensal <= saldoInicial * taxaMensal)
                return null;

            var x = (double)(saqueMensal / taxaMensal) / (double)(saqueMensal / taxaMensal - saldoInicial);
            var n = Math.Log(x) / Math.Log(1 + (double)taxaMensal);
            return (int)Math.Ceiling(n);
        }

        /// <summary>
        /// Simula a evolução mês a mês do saldo durante a retirada, incluindo o
        /// IR retido em cada saque (proporcional ao ganho embutido, conforme o
        /// regime tributário da categoria). Para no mês em que o saldo se esgota.
        /// </summary>
        public static ResultadoRetirada Simular(
            decimal saldoInicial, decimal baseCustoInicial, decimal saqueMensal,
            decimal taxaJurosAnualPercentual, int meses, CategoriaTributariaAtivo categoria)
        {
            ValidarComuns(saldoInicial, taxaJurosAnualPercentual);

            if (baseCustoInicial < 0 || baseCustoInicial > saldoInicial)
                throw new ArgumentException("A base de custo deve estar entre zero e o saldo inicial.", nameof(baseCustoInicial));

            if (saqueMensal <= 0)
                throw new ArgumentException("O saque mensal deve ser maior que zero.", nameof(saqueMensal));

            if (meses <= 0)
                throw new ArgumentException("O prazo em meses deve ser maior que zero.", nameof(meses));

            var taxaMensal = TaxaMensal(taxaJurosAnualPercentual);
            var saldo = saldoInicial;
            var baseCusto = baseCustoInicial;
            var evolucao = new List<MesRetirada>(meses);
            int? mesEsgotamento = null;

            for (var mes = 1; mes <= meses; mes++)
            {
                var saldoAntesDoSaque = Math.Round(saldo * (1 + taxaMensal), 2);
                var saqueEfetivo = Math.Min(saqueMensal, Math.Max(saldoAntesDoSaque, 0m));

                var fracaoGanho = saldoAntesDoSaque > 0
                    ? Math.Max(0m, (saldoAntesDoSaque - baseCusto) / saldoAntesDoSaque)
                    : 0m;
                var ganhoDoSaque = Math.Round(saqueEfetivo * fracaoGanho, 2);

                var (aliquota, valorImposto) = CalcularImposto(categoria, ganhoDoSaque, saqueEfetivo);

                var custoConsumido = saqueEfetivo - ganhoDoSaque;
                baseCusto = Math.Max(0m, baseCusto - custoConsumido);

                var saldoFinal = Math.Round(saldoAntesDoSaque - saqueEfetivo, 2);

                evolucao.Add(new MesRetirada(
                    mes, saldoAntesDoSaque, saqueEfetivo, aliquota, valorImposto, saqueEfetivo - valorImposto, saldoFinal));

                saldo = saldoFinal;
                if (saldo <= 0)
                {
                    mesEsgotamento = mes;
                    break;
                }
            }

            return new ResultadoRetirada(mesEsgotamento.HasValue, mesEsgotamento, evolucao);
        }

        private static (decimal Aliquota, decimal Valor) CalcularImposto(CategoriaTributariaAtivo categoria, decimal ganho, decimal valorSaque)
        {
            switch (categoria)
            {
                case CategoriaTributariaAtivo.RendaFixaIsenta:
                    return (0m, 0m);

                case CategoriaTributariaAtivo.RendaFixaTributavel:
                case CategoriaTributariaAtivo.FundoComeCotasLongoPrazo:
                case CategoriaTributariaAtivo.FundoComeCotasCurtoPrazo:
                {
                    var ir = ImpostoRendaCalculator.Calcular(ganho, valorSaque, PrazoParaAliquotaMinima, isento: false);
                    return (ir.AliquotaPercentual, ir.ValorImposto);
                }

                case CategoriaTributariaAtivo.PrevidenciaPgbl:
                case CategoriaTributariaAtivo.PrevidenciaVgbl:
                {
                    var previdencia = PrevidenciaCalculator.Calcular(ganho, valorSaque, PrazoParaAliquotaMinima, categoria);
                    return (previdencia.AliquotaPercentual, previdencia.ValorImposto);
                }

                default:
                {
                    var ganhoCapital = GanhoCapitalCalculator.Calcular(ganho, valorSaque, categoria);
                    return (ganhoCapital.AliquotaPercentual, ganhoCapital.ValorImposto);
                }
            }
        }

        private static void ValidarComuns(decimal saldoInicial, decimal taxaJurosAnualPercentual)
        {
            if (saldoInicial <= 0)
                throw new ArgumentException("O saldo inicial deve ser maior que zero.", nameof(saldoInicial));

            if (taxaJurosAnualPercentual < 0)
                throw new ArgumentException("A taxa de juros anual não pode ser negativa.", nameof(taxaJurosAnualPercentual));
        }

        private static decimal TaxaMensal(decimal taxaJurosAnualPercentual) =>
            (decimal)Math.Pow((double)(1 + taxaJurosAnualPercentual / 100), 1.0 / 12) - 1;
    }
}
