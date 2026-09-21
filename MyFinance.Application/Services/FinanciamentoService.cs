using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Financiamento;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Services
{
    /// <summary>
    /// Simula um financiamento nos dois sistemas de amortização mais comuns no
    /// Brasil — Price (parcelas fixas) e SAC (amortização constante) — para o
    /// mesmo empréstimo, e aponta qual dos dois custa menos em juros totais.
    /// Também converte taxas nominais anuais (APR) em taxas efetivas (EAR).
    /// </summary>
    public class FinanciamentoService : IFinanciamentoService
    {
        public Task<FinanciamentoResponseDto> SimularAsync(FinanciamentoRequestDto request)
        {
            var composicao = ResolverComposicao(request);
            var valorFinanciado = composicao.ValorFinanciado;
            var amortizacaoExtra = ResolverAmortizacaoExtra(request);
            var encargos = ResolverEncargos(request, composicao);

            var price = FinanciamentoPriceCalculator.Calcular(
                valorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas, amortizacaoExtra, encargos);
            var sac = FinanciamentoSacCalculator.Calcular(
                valorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas, amortizacaoExtra, encargos);

            // A economia só faz sentido contra o mesmo contrato sem pagamento
            // extra — por isso a simulação-base roda de novo, sem extras. Sem
            // amortização extra configurada ela seria idêntica, e aí nem vale
            // o custo de calcular.
            var temExtra = amortizacaoExtra is not null;
            var priceBase = temExtra
                ? FinanciamentoPriceCalculator.Calcular(
                    valorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas, null, encargos)
                : price;
            var sacBase = temExtra
                ? FinanciamentoSacCalculator.Calcular(
                    valorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas, null, encargos)
                : sac;

            var sistemaMaisBarato = price.TotalJuros <= sac.TotalJuros
                ? SistemaAmortizacao.Price
                : SistemaAmortizacao.Sac;
            var diferenca = Math.Round(Math.Abs(price.TotalJuros - sac.TotalJuros), 2);

            if (request.TarifasContratacao < 0)
                throw new ArgumentException(
                    "As tarifas de contratação não podem ser negativas.", nameof(request.TarifasContratacao));

            var cetPrice = CalcularCet(valorFinanciado, request.TarifasContratacao, price.Parcelas);
            var cetSac = CalcularCet(valorFinanciado, request.TarifasContratacao, sac.Parcelas);

            var resposta = new FinanciamentoResponseDto
            {
                Price = new ResultadoFinanciamentoDto
                {
                    PrimeiraParcela = price.ValorParcela,
                    UltimaParcela = price.Parcelas[^1].ValorParcela,
                    TotalPago = price.TotalPago,
                    TotalJuros = price.TotalJuros,
                    JurosSobreFinanciadoPercentual = price.JurosSobreFinanciadoPercentual,
                    Parcelas = MapearParcelas(price.Parcelas),
                    PrazoFinalMeses = price.PrazoFinalMeses,
                    TotalAmortizacaoExtra = price.TotalAmortizacaoExtra,
                    EconomiaJuros = Math.Round(priceBase.TotalJuros - price.TotalJuros, 2),
                    MesesEconomizados = priceBase.PrazoFinalMeses - price.PrazoFinalMeses,
                    TotalSeguros = price.TotalSeguros,
                    TotalTaxaAdministracao = price.TotalTaxaAdministracao,
                    PrimeiraParcelaTotal = price.Parcelas[0].ParcelaTotal,
                    TotalDesembolsado = price.TotalDesembolsado,
                    CetMensalPercentual = cetPrice.TaxaMensalPercentual,
                    CetAnualPercentual = cetPrice.TaxaAnualPercentual,
                    CetConvergiu = cetPrice.Convergiu
                },
                Sac = new ResultadoFinanciamentoDto
                {
                    PrimeiraParcela = sac.PrimeiraParcela,
                    UltimaParcela = sac.UltimaParcela,
                    TotalPago = sac.TotalPago,
                    TotalJuros = sac.TotalJuros,
                    JurosSobreFinanciadoPercentual = sac.JurosSobreFinanciadoPercentual,
                    Parcelas = MapearParcelas(sac.Parcelas),
                    PrazoFinalMeses = sac.PrazoFinalMeses,
                    TotalAmortizacaoExtra = sac.TotalAmortizacaoExtra,
                    EconomiaJuros = Math.Round(sacBase.TotalJuros - sac.TotalJuros, 2),
                    MesesEconomizados = sacBase.PrazoFinalMeses - sac.PrazoFinalMeses,
                    TotalSeguros = sac.TotalSeguros,
                    TotalTaxaAdministracao = sac.TotalTaxaAdministracao,
                    PrimeiraParcelaTotal = sac.Parcelas[0].ParcelaTotal,
                    TotalDesembolsado = sac.TotalDesembolsado,
                    CetMensalPercentual = cetSac.TaxaMensalPercentual,
                    CetAnualPercentual = cetSac.TaxaAnualPercentual,
                    CetConvergiu = cetSac.Convergiu
                },
                SistemaMaisBarato = sistemaMaisBarato,
                DiferencaTotalJuros = diferenca,
                Composicao = new ComposicaoFinanciamentoDto
                {
                    ValorImovel = composicao.ValorImovel,
                    Entrada = composicao.Entrada,
                    EntradaPercentual = composicao.EntradaPercentual,
                    ValorFinanciado = composicao.ValorFinanciado
                }
            };

            return Task.FromResult(resposta);
        }

        public Task<TaxaEfetivaResponseDto> CalcularTaxaEfetivaAsync(TaxaEfetivaRequestDto request)
        {
            var ear = TaxaEfetivaCalculator.Calcular(request.TaxaNominalAnualPercentual, request.CapitalizacoesPorAno);

            return Task.FromResult(new TaxaEfetivaResponseDto
            {
                TaxaNominalAnualPercentual = request.TaxaNominalAnualPercentual,
                CapitalizacoesPorAno = request.CapitalizacoesPorAno,
                TaxaEfetivaAnualPercentual = ear
            });
        }

        /// <summary>
        /// Resolve o valor financiado. Quando o usuário informa o preço do imóvel,
        /// ele é a base e o financiado é o que sobra depois da entrada; quando não,
        /// mantém-se o contrato antigo de receber o financiado já calculado.
        /// </summary>
        private static ComposicaoFinanciamento ResolverComposicao(FinanciamentoRequestDto request)
        {
            if (request.ValorImovel is not null)
                return ComposicaoFinanciamento.Resolver(
                    request.ValorImovel.Value, request.Entrada, request.EntradaPercentual);

            return new ComposicaoFinanciamento(
                request.ValorFinanciado, 0m, 0m, request.ValorFinanciado);
        }

        private static AmortizacaoExtra? ResolverAmortizacaoExtra(FinanciamentoRequestDto request)
        {
            var avulsas = request.AmortizacoesExtrasAvulsas?
                .Select(a => new AmortizacaoExtraAvulsa(a.Mes, a.Valor))
                .ToList();

            var extra = new AmortizacaoExtra(
                request.AmortizacaoExtraMensal, avulsas, request.ModoAmortizacaoExtra);

            return extra.TemAlgumValor ? extra : null;
        }

        /// <summary>
        /// Monta os encargos. O DFI incide sobre o imóvel, então quando o usuário
        /// simula informando só o valor financiado não há base para cobrá-lo.
        /// </summary>
        private static EncargosFinanciamento? ResolverEncargos(
            FinanciamentoRequestDto request, ComposicaoFinanciamento composicao)
        {
            var encargos = new EncargosFinanciamento(
                request.SeguroMipMensalPercentualSaldo,
                request.SeguroDfiMensalPercentualImovel,
                composicao.ValorImovel,
                request.TaxaAdministracaoMensal);

            encargos.Validar();

            return encargos.TemAlgumValor ? encargos : null;
        }

        /// <summary>
        /// Monta o fluxo de caixa do contrato para a TIR: o que o tomador recebe
        /// (financiado menos tarifas de contratação) contra tudo que ele paga —
        /// parcela contratual, extras, seguros e taxa de administração já vêm
        /// somados em <see cref="ParcelaFinanciamento.ParcelaTotal"/>. Entrada,
        /// subsídio e custo de aquisição ficam de fora de propósito: não passam
        /// pelo credor, então não fazem parte do custo do empréstimo em si.
        /// </summary>
        private static CetCalculator.ResultadoCet CalcularCet(
            decimal valorFinanciado, decimal tarifasContratacao, IReadOnlyList<ParcelaFinanciamento> parcelas)
        {
            var fluxo = new List<decimal>(parcelas.Count + 1) { valorFinanciado - tarifasContratacao };
            fluxo.AddRange(parcelas.Select(p => -p.ParcelaTotal));

            return CetCalculator.Calcular(fluxo);
        }

        private static List<ParcelaFinanciamentoDto> MapearParcelas(IReadOnlyList<ParcelaFinanciamento> parcelas) =>
            parcelas.Select(p => new ParcelaFinanciamentoDto
            {
                Numero = p.Numero,
                ValorParcela = p.ValorParcela,
                Juros = p.Juros,
                Amortizacao = p.Amortizacao,
                SaldoDevedor = p.SaldoDevedor,
                AmortizacaoExtra = p.AmortizacaoExtra,
                SeguroMip = p.SeguroMip,
                SeguroDfi = p.SeguroDfi,
                TaxaAdministracao = p.TaxaAdministracao,
                ParcelaTotal = p.ParcelaTotal
            }).ToList();
    }
}
