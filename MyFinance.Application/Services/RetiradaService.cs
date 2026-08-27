using System;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Services
{
    /// <summary>
    /// Fase de retirada (desacumulação): simula saques mensais fixos sobre um
    /// saldo que continua rendendo, com IR retido proporcionalmente ao ganho de
    /// cada saque conforme o regime tributário do tipo de ativo.
    /// </summary>
    public class RetiradaService : IRetiradaService
    {
        private readonly ITaxasReferenciaIntegrationService _taxasReferenciaService;

        /// <summary>Janela de meses exibida na evolução quando o saque é sustentável para sempre (30 anos).</summary>
        private const int MesesEvolucaoDuraParaSempre = 360;

        public RetiradaService(ITaxasReferenciaIntegrationService taxasReferenciaService)
        {
            _taxasReferenciaService = taxasReferenciaService;
        }

        private record TaxaResolvida(decimal Taxa, decimal? CdiAnual, decimal? PercentualCdi);

        public async Task<RetiradaResponseDto> CalcularSaqueSustentavelAsync(CalcularSaqueSustentavelRequestDto request)
        {
            ValidarBaseCusto(request.SaldoInicial, request.BaseCustoInicial);

            var resolvida = await ResolverTaxaAsync(request.FonteTaxaJuros, request.TaxaJurosAnualPercentual, request.PercentualCdi);
            var categoria = TipoAtivoCalculadoraClassificador.Classificar(request.TipoAtivo);

            var saque = RetiradaCalculator.CalcularSaqueMaximoSustentavel(request.SaldoInicial, resolvida.Taxa, request.PrazoMeses);
            var resultado = RetiradaCalculator.Simular(
                request.SaldoInicial, request.BaseCustoInicial, saque, resolvida.Taxa, request.PrazoMeses, categoria);

            var resposta = MontarResposta(saque, resolvida, resultado);
            // O saque foi dimensionado para durar exatamente request.PrazoMeses — o
            // arredondamento a centavos deixa um resíduo desprezível em vez de
            // esgotar de fato, então o rótulo correto aqui nunca é "dura para sempre".
            resposta.DuraParaSempre = false;
            resposta.MesEsgotamento ??= request.PrazoMeses;
            return resposta;
        }

        public async Task<RetiradaResponseDto> CalcularDuracaoAsync(CalcularDuracaoRetiradaRequestDto request)
        {
            ValidarBaseCusto(request.SaldoInicial, request.BaseCustoInicial);

            var resolvida = await ResolverTaxaAsync(request.FonteTaxaJuros, request.TaxaJurosAnualPercentual, request.PercentualCdi);
            var categoria = TipoAtivoCalculadoraClassificador.Classificar(request.TipoAtivo);

            var mesesAteEsgotar = RetiradaCalculator.CalcularMesesAteEsgotar(request.SaldoInicial, request.SaqueMensal, resolvida.Taxa);
            var mesesParaSimular = mesesAteEsgotar ?? MesesEvolucaoDuraParaSempre;

            var resultado = RetiradaCalculator.Simular(
                request.SaldoInicial, request.BaseCustoInicial, request.SaqueMensal, resolvida.Taxa, mesesParaSimular, categoria);

            return MontarResposta(request.SaqueMensal, resolvida, resultado);
        }

        private static void ValidarBaseCusto(decimal saldoInicial, decimal baseCustoInicial)
        {
            if (baseCustoInicial < 0 || baseCustoInicial > saldoInicial)
                throw new ArgumentException(
                    "A base de custo deve estar entre zero e o saldo inicial.", nameof(baseCustoInicial));
        }

        private static RetiradaResponseDto MontarResposta(
            decimal saqueMensal, TaxaResolvida resolvida, RetiradaCalculator.ResultadoRetirada resultado)
        {
            return new RetiradaResponseDto
            {
                SaqueMensal = saqueMensal,
                DuraParaSempre = !resultado.Esgotou,
                MesEsgotamento = resultado.MesEsgotamento,
                TaxaJurosAnualUtilizada = resolvida.Taxa,
                PercentualCdiUtilizado = resolvida.PercentualCdi,
                CdiAnualUtilizado = resolvida.CdiAnual,
                Evolucao = resultado.Evolucao.Select(m => new MesRetiradaDto
                {
                    Mes = m.Mes,
                    SaldoInicial = m.SaldoInicial,
                    SaqueBruto = m.SaqueBruto,
                    AliquotaImpostoPercentual = m.AliquotaImpostoPercentual,
                    ValorImposto = m.ValorImposto,
                    SaqueLiquido = m.SaqueLiquido,
                    SaldoFinal = m.SaldoFinal
                }).ToList()
            };
        }

        private async Task<TaxaResolvida> ResolverTaxaAsync(FonteTaxaJuros fonte, decimal? taxaManual, decimal? percentualCdi)
        {
            switch (fonte)
            {
                case FonteTaxaJuros.Selic:
                {
                    var taxas = await ObterTaxasReferenciaOuThrowAsync();
                    return new TaxaResolvida(taxas.SelicAnualPct, null, null);
                }

                case FonteTaxaJuros.PercentualCdi:
                {
                    if (percentualCdi is null)
                        throw new ArgumentException("Informe o percentual do CDI.", nameof(percentualCdi));

                    var taxas = await ObterTaxasReferenciaOuThrowAsync();
                    var taxaEfetiva = Math.Round(taxas.CdiAnualPct * percentualCdi.Value / 100, 4);
                    return new TaxaResolvida(taxaEfetiva, taxas.CdiAnualPct, percentualCdi.Value);
                }

                default:
                    if (taxaManual is null)
                        throw new ArgumentException("Informe a taxa de juros anual.", nameof(taxaManual));

                    return new TaxaResolvida(taxaManual.Value, null, null);
            }
        }

        private async Task<TaxasReferenciaDto> ObterTaxasReferenciaOuThrowAsync()
        {
            var taxas = await _taxasReferenciaService.GetTaxasReferenciaAsync();
            if (taxas == null)
                throw new InvalidOperationException(
                    "Não foi possível obter as taxas de referência no momento. Informe uma taxa manualmente ou tente novamente mais tarde.");

            return taxas;
        }
    }
}
