using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Services
{
    public class ProjecaoInvestimentoService : IProjecaoInvestimentoService
    {
        private readonly ITaxasReferenciaIntegrationService _taxasReferenciaService;

        public ProjecaoInvestimentoService(ITaxasReferenciaIntegrationService taxasReferenciaService)
        {
            _taxasReferenciaService = taxasReferenciaService;
        }

        /// <summary>
        /// Resultado da resolução da taxa de juros a simular. Os campos de CDI/IPCA
        /// só são preenchidos quando a taxa depende de dados do Banco Central —
        /// no modo manual a calculadora não depende de nenhuma consulta externa.
        /// </summary>
        private record TaxaResolvida(decimal Taxa, decimal? CdiAnual, decimal? PercentualCdi, decimal? IpcaAnual, decimal? RentabilidadeReal);

        public async Task<ProjecaoInvestimentoResponseDto> CalcularProjecaoAsync(CalcularProjecaoRequestDto request)
        {
            var resolvida = await ResolverTaxaAsync(request);
            decimal reajusteAnual;
            (reajusteAnual, resolvida) = await ResolverReajusteAnualAsync(request, resolvida);
            var aportesExtras = request.AportesExtras?.Select(a => new AporteExtra(a.Mes, a.Valor)).ToList();
            var categoria = TipoAtivoCalculadoraClassificador.Classificar(request.TipoAtivo);

            if (categoria is CategoriaTributariaAtivo.FundoComeCotasLongoPrazo or CategoriaTributariaAtivo.FundoComeCotasCurtoPrazo)
                return MontarRespostaComeCotas(request, resolvida, categoria, aportesExtras, reajusteAnual);

            var resultado = ProjecaoInvestimentoCalculator.Calcular(
                request.AporteInicial, request.AporteMensal, resolvida.Taxa, request.PrazoMeses, aportesExtras, reajusteAnual);

            decimal aliquotaIof = 0, valorIof = 0;
            decimal aliquotaImposto = 0, valorImposto = 0;
            var valorFinalLiquido = resultado.ValorFinal;
            var isentoPorFaixaDeVenda = false;

            switch (categoria)
            {
                case CategoriaTributariaAtivo.RendaFixaTributavel:
                {
                    // Prazo aproximado em dias corridos, usado só para a faixa de IOF
                    // (o prazo simulado é sempre em meses inteiros, então o IOF só
                    // aparece nos meses mais curtos, próximos do limiar de 30 dias).
                    var diasCorridos = request.PrazoMeses * 30;

                    var iof = IofCalculator.Calcular(resultado.TotalJuros, diasCorridos);
                    var jurosAposIof = resultado.TotalJuros - iof.ValorIof;
                    var valorAposIof = resultado.ValorFinal - iof.ValorIof;

                    var ir = ImpostoRendaCalculator.Calcular(jurosAposIof, valorAposIof, request.PrazoMeses, isento: false);

                    aliquotaIof = iof.AliquotaPercentual;
                    valorIof = iof.ValorIof;
                    aliquotaImposto = ir.AliquotaPercentual;
                    valorImposto = ir.ValorImposto;
                    valorFinalLiquido = ir.ValorLiquido;
                    break;
                }

                case CategoriaTributariaAtivo.RendaFixaIsenta:
                    // Sem IOF, sem IR — valorFinalLiquido já é resultado.ValorFinal.
                    break;

                case CategoriaTributariaAtivo.PrevidenciaPgbl:
                case CategoriaTributariaAtivo.PrevidenciaVgbl:
                {
                    var previdencia = PrevidenciaCalculator.Calcular(resultado.TotalJuros, resultado.ValorFinal, request.PrazoMeses, categoria);

                    aliquotaImposto = previdencia.AliquotaPercentual;
                    valorImposto = previdencia.ValorImposto;
                    valorFinalLiquido = previdencia.ValorLiquido;
                    break;
                }

                default:
                {
                    // Categorias de ganho de capital: Ação, FII, Cripto, Fundo de Ações.
                    var ganhoCapital = GanhoCapitalCalculator.Calcular(resultado.TotalJuros, resultado.ValorFinal, categoria);

                    aliquotaImposto = ganhoCapital.AliquotaPercentual;
                    valorImposto = ganhoCapital.ValorImposto;
                    valorFinalLiquido = ganhoCapital.ValorLiquido;
                    isentoPorFaixaDeVenda = ganhoCapital.Isento && resultado.TotalJuros > 0;
                    break;
                }
            }

            return new ProjecaoInvestimentoResponseDto
            {
                ValorFinal = resultado.ValorFinal,
                TotalAportado = resultado.TotalAportado,
                TotalJuros = resultado.TotalJuros,
                RentabilidadePercentual = resultado.RentabilidadePercentual,
                TaxaJurosAnualUtilizada = resolvida.Taxa,
                PercentualCdiUtilizado = resolvida.PercentualCdi,
                CdiAnualUtilizado = resolvida.CdiAnual,
                IpcaAnualUtilizado = resolvida.IpcaAnual,
                RentabilidadeRealAnualPercentual = resolvida.RentabilidadeReal,
                AliquotaIofPercentual = aliquotaIof,
                ValorIof = valorIof,
                AliquotaImpostoRendaPercentual = aliquotaImposto,
                ValorImpostoRenda = valorImposto,
                ValorFinalLiquido = valorFinalLiquido,
                CategoriaTributaria = categoria,
                IsentoPorFaixaDeVenda = isentoPorFaixaDeVenda,
                Evolucao = resultado.Evolucao.Select(m => new MesProjecaoDto
                {
                    Mes = m.Mes,
                    ValorAcumulado = m.ValorAcumulado,
                    TotalAportadoAcumulado = m.TotalAportadoAcumulado,
                    JurosAcumulado = m.JurosAcumulado
                }).ToList()
            };
        }

        private async Task<TaxaResolvida> ResolverTaxaAsync(CalcularProjecaoRequestDto request)
        {
            switch (request.FonteTaxaJuros)
            {
                case FonteTaxaJuros.Selic:
                {
                    var taxas = await ObterTaxasReferenciaOuThrowAsync();
                    var real = TaxaRealCalculator.Calcular(taxas.SelicAnualPct, taxas.IpcaAnualPct);
                    return new TaxaResolvida(taxas.SelicAnualPct, null, null, taxas.IpcaAnualPct, real);
                }

                case FonteTaxaJuros.PercentualCdi:
                {
                    if (request.PercentualCdi is null)
                        throw new ArgumentException("Informe o percentual do CDI.", nameof(request.PercentualCdi));

                    var taxas = await ObterTaxasReferenciaOuThrowAsync();
                    var taxaEfetiva = Math.Round(taxas.CdiAnualPct * request.PercentualCdi.Value / 100, 4);
                    var real = TaxaRealCalculator.Calcular(taxaEfetiva, taxas.IpcaAnualPct);
                    return new TaxaResolvida(taxaEfetiva, taxas.CdiAnualPct, request.PercentualCdi.Value, taxas.IpcaAnualPct, real);
                }

                default:
                    if (request.TaxaJurosAnualPercentual is null)
                        throw new ArgumentException("Informe a taxa de juros anual.", nameof(request.TaxaJurosAnualPercentual));

                    return new TaxaResolvida(request.TaxaJurosAnualPercentual.Value, null, null, null, null);
            }
        }

        /// <summary>
        /// Resolve o percentual de reajuste anual do aporte mensal. No modo IPCA,
        /// busca a inflação via Banco Central caso ainda não tenha sido buscada
        /// para a taxa de juros (ex.: taxa manual + reajuste por IPCA).
        /// </summary>
        private async Task<(decimal ReajusteAnual, TaxaResolvida Resolvida)> ResolverReajusteAnualAsync(
            CalcularProjecaoRequestDto request, TaxaResolvida resolvida)
        {
            switch (request.ReajusteAporteModo)
            {
                case ReajusteAporteModo.PercentualFixo:
                    if (request.ReajusteAporteAnualPercentual is null)
                        throw new ArgumentException(
                            "Informe o percentual de reajuste anual do aporte.", nameof(request.ReajusteAporteAnualPercentual));

                    return (request.ReajusteAporteAnualPercentual.Value, resolvida);

                case ReajusteAporteModo.Ipca:
                    if (resolvida.IpcaAnual is not null)
                        return (resolvida.IpcaAnual.Value, resolvida);

                    var taxas = await ObterTaxasReferenciaOuThrowAsync();
                    return (taxas.IpcaAnualPct, resolvida with { IpcaAnual = taxas.IpcaAnualPct });

                default:
                    return (0m, resolvida);
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

        /// <summary>
        /// Fundos com come-cotas usam um núcleo de simulação diferente (que
        /// retém IR semestralmente ao longo da evolução), por isso montam a
        /// resposta separadamente do fluxo principal.
        /// </summary>
        private static ProjecaoInvestimentoResponseDto MontarRespostaComeCotas(
            CalcularProjecaoRequestDto request, TaxaResolvida resolvida, CategoriaTributariaAtivo categoria,
            IReadOnlyList<AporteExtra>? aportesExtras, decimal reajusteAnual)
        {
            var aliquotaAntecipacao = categoria == CategoriaTributariaAtivo.FundoComeCotasLongoPrazo ? 15m : 20m;

            var comeCotas = ProjecaoComeCotasCalculator.Calcular(
                request.AporteInicial, request.AporteMensal, resolvida.Taxa, request.PrazoMeses, aliquotaAntecipacao,
                aportesExtras, reajusteAnual);

            // IR total devido sobre todo o ganho histórico (o que já foi retido via
            // come-cotas + o que ainda está na posição), pela tabela regressiva do
            // prazo total. O que já foi antecipado vira crédito contra esse total.
            var valorFinalBrutoEquivalente = comeCotas.ValorFinal + comeCotas.TotalComeCotasRetido;
            var irTotal = ImpostoRendaCalculator.Calcular(
                comeCotas.TotalGanhoBruto, valorFinalBrutoEquivalente, request.PrazoMeses, isento: false);

            var irComplementar = Math.Max(0m, irTotal.ValorImposto - comeCotas.TotalComeCotasRetido);
            var valorFinalLiquido = comeCotas.ValorFinal - irComplementar;

            return new ProjecaoInvestimentoResponseDto
            {
                ValorFinal = comeCotas.ValorFinal,
                TotalAportado = comeCotas.TotalAportado,
                TotalJuros = comeCotas.TotalGanhoBruto,
                RentabilidadePercentual = comeCotas.RentabilidadePercentual,
                TaxaJurosAnualUtilizada = resolvida.Taxa,
                PercentualCdiUtilizado = resolvida.PercentualCdi,
                CdiAnualUtilizado = resolvida.CdiAnual,
                IpcaAnualUtilizado = resolvida.IpcaAnual,
                RentabilidadeRealAnualPercentual = resolvida.RentabilidadeReal,
                AliquotaImpostoRendaPercentual = irTotal.AliquotaPercentual,
                ValorImpostoRenda = irComplementar,
                AliquotaComeCotasPercentual = aliquotaAntecipacao,
                ValorComeCotasRetido = comeCotas.TotalComeCotasRetido,
                ValorFinalLiquido = valorFinalLiquido,
                CategoriaTributaria = categoria,
                Evolucao = comeCotas.Evolucao.Select(m => new MesProjecaoDto
                {
                    Mes = m.Mes,
                    ValorAcumulado = m.ValorAcumulado,
                    TotalAportadoAcumulado = m.TotalAportadoAcumulado,
                    JurosAcumulado = m.JurosAcumulado
                }).ToList()
            };
        }
    }
}
