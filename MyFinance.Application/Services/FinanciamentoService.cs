using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Financiamento;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Services
{
    /// <summary>
    /// Simula um financiamento nos dois sistemas de amortização mais comuns no
    /// Brasil — Price (parcelas fixas) e SAC (amortização constante) — para o
    /// mesmo empréstimo, e aponta qual dos dois custa menos em juros totais.
    /// Também converte taxas nominais anuais (APR) em taxas efetivas (EAR), e
    /// compara amortizar uma dívida a mais contra investir o mesmo dinheiro,
    /// reaproveitando <see cref="IProjecaoInvestimentoService"/> como caixa-preta
    /// para IR/IOF/come-cotas — mesmo padrão do <c>MetaReversaService</c>.
    /// </summary>
    public class FinanciamentoService : IFinanciamentoService
    {
        private readonly IProjecaoInvestimentoService _projecaoService;

        public FinanciamentoService(IProjecaoInvestimentoService projecaoService)
        {
            _projecaoService = projecaoService;
        }

        public Task<FinanciamentoResponseDto> SimularAsync(FinanciamentoRequestDto request)
        {
            ValidarPreCondicoesMcmv(request);

            var subsidio = request.MinhaCasaMinhaVida ? (request.SubsidioInformado ?? 0m) : 0m;
            var composicao = ResolverComposicao(request, subsidio);
            var valorFinanciado = composicao.ValorFinanciado;
            var amortizacaoExtra = ResolverAmortizacaoExtra(request);
            var encargos = ResolverEncargos(request, composicao);

            var faixaMcmv = request.MinhaCasaMinhaVida
                ? MinhaCasaMinhaVidaTabela.ResolverFaixa(request.RendaMensal!.Value)
                : null;
            var taxaMensal = ResolverTaxaMensal(request, faixaMcmv);

            var price = FinanciamentoPriceCalculator.Calcular(
                valorFinanciado, taxaMensal, request.NumParcelas, amortizacaoExtra, encargos);
            var sac = FinanciamentoSacCalculator.Calcular(
                valorFinanciado, taxaMensal, request.NumParcelas, amortizacaoExtra, encargos);

            // A economia só faz sentido contra o mesmo contrato sem pagamento
            // extra — por isso a simulação-base roda de novo, sem extras. Sem
            // amortização extra configurada ela seria idêntica, e aí nem vale
            // o custo de calcular.
            var temExtra = amortizacaoExtra is not null;
            var priceBase = temExtra
                ? FinanciamentoPriceCalculator.Calcular(
                    valorFinanciado, taxaMensal, request.NumParcelas, null, encargos)
                : price;
            var sacBase = temExtra
                ? FinanciamentoSacCalculator.Calcular(
                    valorFinanciado, taxaMensal, request.NumParcelas, null, encargos)
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

            var comprometimentoPrice = ComprometimentoRenda(request.RendaMensal, price.Parcelas[0].ParcelaTotal);
            var comprometimentoSac = ComprometimentoRenda(request.RendaMensal, sac.Parcelas[0].ParcelaTotal);

            var avisos = new List<AvisoFinanciamentoDto>();
            avisos.AddRange(MapearAvisos(FinanciamentoAvisoAvaliador.Avaliar(
                request.RendaMensal, price.Parcelas[0].ParcelaTotal, SistemaAmortizacao.Price)));
            avisos.AddRange(MapearAvisos(FinanciamentoAvisoAvaliador.Avaliar(
                request.RendaMensal, sac.Parcelas[0].ParcelaTotal, SistemaAmortizacao.Sac)));

            if (request.MinhaCasaMinhaVida)
                avisos.AddRange(MapearAvisos(FinanciamentoAvisoAvaliador.AvaliarMcmv(
                    faixaMcmv, request.RendaMensal!.Value, composicao, request.NumParcelas)));

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
                    CetConvergiu = cetPrice.Convergiu,
                    ComprometimentoRendaPercentual = comprometimentoPrice
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
                    CetConvergiu = cetSac.Convergiu,
                    ComprometimentoRendaPercentual = comprometimentoSac
                },
                SistemaMaisBarato = sistemaMaisBarato,
                DiferencaTotalJuros = diferenca,
                Composicao = new ComposicaoFinanciamentoDto
                {
                    ValorImovel = composicao.ValorImovel,
                    Entrada = composicao.Entrada,
                    EntradaPercentual = composicao.EntradaPercentual,
                    ValorFinanciado = composicao.ValorFinanciado,
                    Itbi = composicao.Itbi,
                    CustosCartorio = composicao.CustosCartorio,
                    DesembolsoInicial = composicao.DesembolsoInicial,
                    Subsidio = composicao.Subsidio
                },
                Avisos = avisos,
                FaixaMcmv = MapearFaixa(faixaMcmv),
                VigenciaReferenciaMcmv = request.MinhaCasaMinhaVida ? MinhaCasaMinhaVidaTabela.VigenciaReferencia : null
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
        /// Compara o que compensa mais com o mesmo dinheiro todo mês: abater a
        /// mais no financiamento (prazo reduzido, a estratégia que mais economiza
        /// juros) ou investir. A economia de juros vem de rodar o mesmo cálculo
        /// fechado do financiamento com e sem o extra — não precisa de simulação
        /// assíncrona. O lado do investimento chama <see cref="IProjecaoInvestimentoService"/>
        /// uma única vez, reaproveitando toda a tributação (IR/IOF/come-cotas) já
        /// resolvida lá, sem duplicar essa lógica aqui.
        /// </summary>
        public async Task<AmortizarVsInvestirResponseDto> AmortizarVsInvestirAsync(AmortizarVsInvestirRequestDto request)
        {
            if (request.ValorDisponivelMensal <= 0)
                throw new ArgumentException(
                    "O valor disponível por mês deve ser maior que zero — sem ele não há o que comparar.",
                    nameof(request.ValorDisponivelMensal));

            var extra = new AmortizacaoExtra(request.ValorDisponivelMensal, null, ModoAmortizacaoExtra.ReduzirPrazo);

            decimal totalJurosSemExtra;
            decimal totalJurosComExtra;

            if (request.Sistema == SistemaAmortizacao.Price)
            {
                totalJurosSemExtra = FinanciamentoPriceCalculator.Calcular(
                    request.ValorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas).TotalJuros;
                totalJurosComExtra = FinanciamentoPriceCalculator.Calcular(
                    request.ValorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas, extra).TotalJuros;
            }
            else
            {
                totalJurosSemExtra = FinanciamentoSacCalculator.Calcular(
                    request.ValorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas).TotalJuros;
                totalJurosComExtra = FinanciamentoSacCalculator.Calcular(
                    request.ValorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas, extra).TotalJuros;
            }

            var economiaJuros = Math.Round(totalJurosSemExtra - totalJurosComExtra, 2);

            var projecao = await _projecaoService.CalcularProjecaoAsync(new CalcularProjecaoRequestDto
            {
                AporteInicial = 0m,
                AporteMensal = request.ValorDisponivelMensal,
                PrazoMeses = request.NumParcelas,
                FonteTaxaJuros = request.FonteTaxaJurosInvestimento,
                TaxaJurosAnualPercentual = request.TaxaJurosAnualInvestimentoPercentual,
                PercentualCdi = request.PercentualCdiInvestimento,
                TipoAtivo = request.TipoAtivoInvestimento
            });

            var diferenca = Math.Round(projecao.ValorFinalLiquido - economiaJuros, 2);

            return new AmortizarVsInvestirResponseDto
            {
                Recomendacao = diferenca > 0 ? RecomendacaoFinanceira.Investir : RecomendacaoFinanceira.Amortizar,
                EconomiaJurosAmortizando = economiaJuros,
                ValorFinalLiquidoInvestindo = projecao.ValorFinalLiquido,
                Diferenca = diferenca,
                ProjecaoInvestindo = projecao
            };
        }

        /// <summary>
        /// Resolve o valor financiado. Quando o usuário informa o preço do imóvel,
        /// ele é a base e o financiado é o que sobra depois da entrada (e do
        /// subsídio, quando houver); quando não, mantém-se o contrato antigo de
        /// receber o financiado já calculado.
        /// </summary>
        private static ComposicaoFinanciamento ResolverComposicao(FinanciamentoRequestDto request, decimal subsidio)
        {
            if (request.ValorImovel is not null)
                return ComposicaoFinanciamento.Resolver(
                    request.ValorImovel.Value, request.Entrada, request.EntradaPercentual,
                    request.Itbi, request.CustosCartorio, subsidio);

            // Sem valor de imóvel, o caminho legado não passa pelas validações do
            // Resolver — replicadas aqui só para os campos novos. Subsídio nunca
            // chega não-zero aqui: MCMV exige valor de imóvel (ver ValidarPreCondicoesMcmv).
            if (request.Itbi < 0)
                throw new ArgumentException("O ITBI não pode ser negativo.", nameof(request.Itbi));

            if (request.CustosCartorio < 0)
                throw new ArgumentException(
                    "Os custos de cartório não podem ser negativos.", nameof(request.CustosCartorio));

            return new ComposicaoFinanciamento(
                request.ValorFinanciado, 0m, 0m, request.ValorFinanciado, request.Itbi, request.CustosCartorio, subsidio);
        }

        /// <summary>
        /// Checa o que o MCMV precisa para funcionar antes de qualquer cálculo:
        /// valor do imóvel (a base de tudo no programa) e renda (é ela que resolve
        /// a faixa). Diferente das regras do programa em si — essas viram aviso —
        /// isso aqui é entrada incalculável, então lança.
        /// </summary>
        private static void ValidarPreCondicoesMcmv(FinanciamentoRequestDto request)
        {
            if (!request.MinhaCasaMinhaVida)
                return;

            if (request.ValorImovel is null)
                throw new ArgumentException(
                    "Informe o valor do imóvel para simular pelo Minha Casa Minha Vida.", nameof(request.ValorImovel));

            if (request.RendaMensal is not (> 0))
                throw new ArgumentException(
                    "Informe a renda familiar para simular pelo Minha Casa Minha Vida — é ela que resolve a faixa.",
                    nameof(request.RendaMensal));
        }

        /// <summary>
        /// Resolve a taxa mensal: a do usuário quando informada, senão o teto da
        /// faixa do MCMV resolvida. Sem nenhuma das duas, não há como simular —
        /// essa é uma entrada incalculável, não uma regra do programa, então lança.
        /// </summary>
        private static decimal ResolverTaxaMensal(FinanciamentoRequestDto request, FaixaMcmv? faixaMcmv)
        {
            if (request.TaxaJurosMensalPercentual is { } taxa)
                return taxa;

            if (faixaMcmv is not null)
                return MinhaCasaMinhaVidaTabela.TaxaTetoMensalPercentual(faixaMcmv);

            throw new ArgumentException(
                "Informe a taxa de juros do contrato — sem ela e sem uma faixa do MCMV resolvida, não há como simular.",
                nameof(request.TaxaJurosMensalPercentual));
        }

        private static FaixaMcmvDto? MapearFaixa(FaixaMcmv? faixa) =>
            faixa is null
                ? null
                : new FaixaMcmvDto
                {
                    Faixa = faixa.Faixa.ToString(),
                    RendaMaxima = faixa.RendaMaxima,
                    TaxaAnualMinimaPercentual = faixa.TaxaAnualMinimaPercentual,
                    TaxaAnualMaximaPercentual = faixa.TaxaAnualMaximaPercentual,
                    TetoImovelReferencia = faixa.TetoImovelReferencia,
                    EntradaMinimaPercentual = faixa.EntradaMinimaPercentual,
                    SubsidioMaximoPercentualImovel = faixa.SubsidioMaximoPercentualImovel,
                    SubsidioMaximoValor = faixa.SubsidioMaximoValor
                };

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

        private static decimal ComprometimentoRenda(decimal? rendaMensal, decimal primeiraParcelaTotal) =>
            rendaMensal is > 0 ? Math.Round(primeiraParcelaTotal / rendaMensal.Value * 100, 2) : 0m;

        private static IEnumerable<AvisoFinanciamentoDto> MapearAvisos(IEnumerable<AvisoFinanciamento> avisos) =>
            avisos.Select(a => new AvisoFinanciamentoDto
            {
                Codigo = a.Codigo,
                Mensagem = a.Mensagem,
                Severidade = a.Severidade.ToString()
            });

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
