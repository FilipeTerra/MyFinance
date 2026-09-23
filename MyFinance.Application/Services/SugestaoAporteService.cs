using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Sugestao;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Services;

/// <summary>
/// Monta a sugestão de comportamento financeiro da meta reversa: apura o orçamento
/// mensal do usuário a partir do histórico, delega o diagnóstico a
/// <see cref="SugestaoAporteCalculator"/> e enriquece o resultado com reserva de
/// emergência, cenários alternativos e um texto consultivo.
/// </summary>
/// <remarks>
/// A IA é opcional em todo o fluxo, como na importação de extrato: ela só redige o
/// texto por cima de números já calculados. Agente fora do ar (ou respondendo com
/// valor que não foi enviado como fato) cai no template determinístico e marca
/// <c>IaIndisponivel</c> — o diagnóstico continua completo.
/// </remarks>
public class SugestaoAporteService : ISugestaoAporteService
{
    /// <summary>Meses completos de histórico considerados na média mensal.</summary>
    private const int MesesDeHistorico = 6;

    /// <summary>Abaixo disso as médias são ruído, e a UI precisa avisar.</summary>
    private const int MesesMinimosConfiaveis = 2;

    private static readonly CultureInfo CulturaBrasil = new("pt-BR");

    /// <summary>
    /// Captura valores monetários citados pelo texto do agente. Só o que vem colado a
    /// "R$" é verificado: exigir que todo número da resposta esteja na lista de fatos
    /// reprovaria frases legítimas como "nos próximos 6 meses".
    /// </summary>
    private static readonly Regex ValorMonetario = new(@"R\$\s*([\d.,]+)", RegexOptions.Compiled);

    private readonly IUserRepository _userRepository;
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IFinancialGoalRepository _goalRepository;
    private readonly IInvestimentoRepository _investimentoRepository;
    private readonly IMetaReversaService _metaReversaService;
    private readonly IProjecaoInvestimentoService _projecaoService;
    private readonly IAiIntegrationService _aiIntegrationService;

    public SugestaoAporteService(
        IUserRepository userRepository,
        IAnalyticsRepository analyticsRepository,
        IFinancialGoalRepository goalRepository,
        IInvestimentoRepository investimentoRepository,
        IMetaReversaService metaReversaService,
        IProjecaoInvestimentoService projecaoService,
        IAiIntegrationService aiIntegrationService)
    {
        _userRepository = userRepository;
        _analyticsRepository = analyticsRepository;
        _goalRepository = goalRepository;
        _investimentoRepository = investimentoRepository;
        _metaReversaService = metaReversaService;
        _projecaoService = projecaoService;
        _aiIntegrationService = aiIntegrationService;
    }

    public async Task<ServiceResponse<SugestaoAporteResponseDto>> ObterSugestaoAsync(
        Guid userId, SugestaoAporteRequestDto request)
    {
        if (request.ValorAlvo <= 0)
            return Falha("O valor-alvo deve ser maior que zero.");

        if (request.PrazoMeses <= 0)
            return Falha("O prazo em meses deve ser maior que zero.");

        if (request.AporteMensalNecessario is < 0)
            return Falha("O aporte mensal necessário não pode ser negativo.");

        var (inicio, fim) = JanelaDeAnalise(DateTime.UtcNow);

        var usuario = await _userRepository.GetUserByIdAsync(userId);
        if (usuario is null)
            return Falha("Usuário não encontrado.");

        var totaisPorCategoria = (await _analyticsRepository
            .GetMonthlyCategoryTotalsAsync(userId, inicio, fim, null)).ToList();
        var fluxoMensal = (await _analyticsRepository
            .GetMonthlyFlowAsync(userId, inicio, fim, null)).ToList();
        var aportesMensais = (await _analyticsRepository
            .GetMonthlyInvestmentTotalsAsync(userId, inicio, fim, null)).ToList();

        // Divide-se pelos meses que realmente têm lançamento, não pelos 6 fixos: quem
        // usa o app há dois meses teria a média diluída à metade se fosse pelo período.
        var mesesAnalisados = ContarMesesComDados(totaisPorCategoria, fluxoMensal, aportesMensais);
        var divisor = Math.Max(mesesAnalisados, 1);

        var (rendaMensal, fonteRenda) = ResolverRenda(usuario.MonthlyIncome, fluxoMensal, divisor);
        if (fonteRenda == FonteRenda.Indisponivel)
        {
            return Falha(
                "Não há como sugerir sem saber quanto você ganha: cadastre seu salário no perfil "
                + "ou registre as receitas no extrato.");
        }

        var gastos = AgruparGastosPorCategoria(totaisPorCategoria, divisor);
        var aportesMedios = Math.Round(aportesMensais.Sum(a => a.Total) / divisor, 2);

        var aporteNecessario = request.AporteMensalNecessario
            ?? (await _metaReversaService.CalcularAporteNecessarioAsync(ParaMetaReversa(request)))
                .AporteMensalNecessario;

        var perfil = new SugestaoAporteCalculator.PerfilFinanceiroMensal(rendaMensal, gastos, aportesMedios);
        var diagnostico = SugestaoAporteCalculator.Calcular(perfil, aporteNecessario);

        var reserva = await CalcularReservaAsync(userId, rendaMensal);
        var cenarios = diagnostico.Cabe ? null : await CalcularCenariosAsync(request, diagnostico.CapacidadeMaxima);

        var resposta = new SugestaoAporteResponseDto
        {
            AporteMensalNecessario = aporteNecessario,
            Cabe = diagnostico.Cabe,
            RendaMensal = Math.Round(rendaMensal, 2),
            FonteRenda = fonteRenda,
            DespesaMensalMedia = Math.Round(diagnostico.DespesaTotal, 2),
            AportesMensaisMedios = aportesMedios,
            SobraLivre = Math.Round(diagnostico.SobraLivre, 2),
            Deficit = diagnostico.Deficit,
            FolgaRestante = Math.Round(diagnostico.FolgaRestante, 2),
            CapacidadeMaxima = diagnostico.CapacidadeMaxima,
            PercentualDaRenda = diagnostico.PercentualDaRenda,
            Cortes = diagnostico.Cortes
                .Select(c => new CorteSugeridoDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.Nome,
                    GastoAtual = Math.Round(c.GastoAtual, 2),
                    ValorCorte = c.ValorCorte,
                    GastoDepoisDoCorte = Math.Round(c.GastoDepoisDoCorte, 2),
                })
                .ToList(),
            NaoClassificadas = diagnostico.NaoClassificadas
                .Select(c => new CategoriaNaoClassificadaDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.Nome,
                    GastoMensalMedio = Math.Round(c.MediaMensal, 2),
                })
                .ToList(),
            Reserva = reserva,
            Cenarios = cenarios,
            InicioAnalise = inicio,
            FimAnalise = fim,
            MesesAnalisados = mesesAnalisados,
            HistoricoInsuficiente = mesesAnalisados < MesesMinimosConfiaveis,
        };

        await PreencherTextoConsultivoAsync(resposta);

        return new ServiceResponse<SugestaoAporteResponseDto> { Data = resposta };
    }

    /// <summary>
    /// Últimos <see cref="MesesDeHistorico"/> meses completos. O mês corrente fica de
    /// fora de propósito: meio mês de gasto lançado passaria a falsa impressão de sobra.
    /// </summary>
    private static (DateTime Inicio, DateTime Fim) JanelaDeAnalise(DateTime agora)
    {
        var primeiroDiaDoMesCorrente = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = primeiroDiaDoMesCorrente.AddDays(-1);
        var inicio = primeiroDiaDoMesCorrente.AddMonths(-MesesDeHistorico);
        return (inicio, fim);
    }

    private static int ContarMesesComDados(
        IEnumerable<Dtos.Analytics.MonthlyCategoryTotalDto> gastos,
        IEnumerable<Dtos.Analytics.MonthlyFlowDto> fluxo,
        IEnumerable<Dtos.Analytics.MonthlyInvestmentTotalDto> aportes)
    {
        var meses = new HashSet<(int, int)>();
        foreach (var g in gastos) meses.Add((g.Year, g.Month));
        foreach (var f in fluxo) meses.Add((f.Year, f.Month));
        foreach (var a in aportes) meses.Add((a.Year, a.Month));
        return meses.Count;
    }

    /// <summary>
    /// Prefere o salário declarado no perfil; sem ele, cai para a média das receitas
    /// efetivamente lançadas. O resultado diz qual fonte foi usada, para a UI explicar
    /// de onde veio o número.
    /// </summary>
    private static (decimal Renda, FonteRenda Fonte) ResolverRenda(
        decimal? salarioDeclarado,
        IReadOnlyList<Dtos.Analytics.MonthlyFlowDto> fluxo,
        int divisor)
    {
        if (salarioDeclarado is > 0)
            return (salarioDeclarado.Value, FonteRenda.PerfilDeclarado);

        var mediaRealizada = fluxo.Sum(f => f.TotalIncome) / divisor;
        return mediaRealizada > 0
            ? (Math.Round(mediaRealizada, 2), FonteRenda.TransacoesRealizadas)
            : (0m, FonteRenda.Indisponivel);
    }

    private static IReadOnlyList<SugestaoAporteCalculator.GastoCategoria> AgruparGastosPorCategoria(
        IReadOnlyList<Dtos.Analytics.MonthlyCategoryTotalDto> totais, int divisor)
    {
        return totais
            .GroupBy(t => t.CategoryId)
            .Select(g => new SugestaoAporteCalculator.GastoCategoria(
                g.Key,
                g.First().CategoryName,
                Math.Round(g.Sum(t => t.Total) / divisor, 2),
                g.First().Nature))
            .Where(g => g.MediaMensal > 0)
            .OrderByDescending(g => g.MediaMensal)
            .ToList();
    }

    private async Task<ReservaEmergenciaDto?> CalcularReservaAsync(Guid userId, decimal rendaMensal)
    {
        var metas = await _goalRepository.GetAllByUserIdAsync(userId);
        var investimentos = await _investimentoRepository.GetAllByUserIdAsync(userId);

        var emRendaFixa = investimentos
            .Where(i => i.Tipo == InvestmentType.RendaFixa)
            .Sum(i => i.ValorAtual);

        var resultado = ReservaEmergenciaCalculator.Calcular(
            rendaMensal,
            metas.Select(m => new ReservaEmergenciaCalculator.MetaGuardada(m.Name, m.CurrentAmount)).ToList(),
            emRendaFixa);

        if (resultado is null)
            return null;

        return new ReservaEmergenciaDto
        {
            Adequada = resultado.Adequada,
            ValorIdeal = resultado.ValorIdeal,
            ValorAtual = resultado.ValorAtual,
            ValorFaltante = resultado.ValorFaltante,
            PercentualAtingido = resultado.PercentualAtingido,
            MesesCobertos = resultado.MesesCobertos,
        };
    }

    /// <summary>
    /// Duas saídas para quem não consegue bancar o aporte pedido: esticar o prazo
    /// mantendo o alvo, ou manter o prazo e aceitar um alvo menor.
    /// </summary>
    private async Task<CenariosAlternativosDto> CalcularCenariosAsync(
        SugestaoAporteRequestDto request, decimal capacidadeMaxima)
    {
        var aporteSustentavel = Math.Max(Math.Round(capacidadeMaxima, 2), 0m);
        var cenarios = new CenariosAlternativosDto { AporteSustentavel = aporteSustentavel };

        var prazo = await _metaReversaService.CalcularPrazoNecessarioAsync(new CalcularPrazoNecessarioRequestDto
        {
            AporteInicial = request.AporteInicial,
            AporteMensal = aporteSustentavel,
            ValorAlvo = request.ValorAlvo,
            FonteTaxaJuros = request.FonteTaxaJuros,
            TaxaJurosAnualPercentual = request.TaxaJurosAnualPercentual,
            PercentualCdi = request.PercentualCdi,
            TipoAtivo = request.TipoAtivo,
        });

        cenarios.PrazoMesesAlternativo = prazo.Atingivel ? prazo.PrazoMesesNecessario : null;
        cenarios.AlvoInatingivelNoPrazoMaximo = !prazo.Atingivel;

        // Uma única projeção resolve o alvo alternativo: o valor líquido final do aporte
        // sustentável no prazo original já é a resposta, sem precisar de busca binária.
        var projecao = await _projecaoService.CalcularProjecaoAsync(new CalcularProjecaoRequestDto
        {
            AporteInicial = request.AporteInicial,
            AporteMensal = aporteSustentavel,
            PrazoMeses = request.PrazoMeses,
            FonteTaxaJuros = request.FonteTaxaJuros,
            TaxaJurosAnualPercentual = request.TaxaJurosAnualPercentual,
            PercentualCdi = request.PercentualCdi,
            TipoAtivo = request.TipoAtivo,
        });

        cenarios.ValorAlvoAlternativo = Math.Round(projecao.ValorFinalLiquido, 2);
        return cenarios;
    }

    private static CalcularAporteNecessarioRequestDto ParaMetaReversa(SugestaoAporteRequestDto request) => new()
    {
        AporteInicial = request.AporteInicial,
        PrazoMeses = request.PrazoMeses,
        ValorAlvo = request.ValorAlvo,
        FonteTaxaJuros = request.FonteTaxaJuros,
        TaxaJurosAnualPercentual = request.TaxaJurosAnualPercentual,
        PercentualCdi = request.PercentualCdi,
        TipoAtivo = request.TipoAtivo,
    };

    /// <summary>
    /// Tenta a redação do agente de IA e valida que ela não inventou nenhum valor.
    /// Qualquer falha cai no template determinístico — texto mais seco, diagnóstico igual.
    /// </summary>
    private async Task PreencherTextoConsultivoAsync(SugestaoAporteResponseDto resposta)
    {
        var fatos = MontarFatos(resposta);
        var texto = await _aiIntegrationService.NarrateSuggestionAsync(fatos);

        if (!string.IsNullOrWhiteSpace(texto) && SomenteValoresConhecidos(texto, ValoresPermitidos(resposta)))
        {
            resposta.TextoConsultivo = texto!;
            resposta.IaUsada = true;
            return;
        }

        resposta.TextoConsultivo = MontarTextoTemplate(resposta);
        resposta.IaIndisponivel = true;
    }

    private static SuggestionFactsDto MontarFatos(SugestaoAporteResponseDto r) => new()
    {
        AporteMensalNecessario = r.AporteMensalNecessario,
        Cabe = r.Cabe,
        RendaMensal = r.RendaMensal,
        DespesaMensalMedia = r.DespesaMensalMedia,
        SobraLivre = r.SobraLivre,
        Deficit = r.Deficit,
        FolgaRestante = r.FolgaRestante,
        PercentualDaRenda = r.PercentualDaRenda,
        Cortes = r.Cortes.Select(c => new CorteFatoDto(c.CategoryName, c.ValorCorte)).ToList(),
        ReservaFaltante = r.Reserva is { Adequada: false } ? r.Reserva.ValorFaltante : null,
        PrazoMesesAlternativo = r.Cenarios?.PrazoMesesAlternativo,
        ValorAlvoAlternativo = r.Cenarios?.ValorAlvoAlternativo,
    };

    /// <summary>Todo valor em reais que o agente tem permissão para citar.</summary>
    private static IReadOnlyCollection<decimal> ValoresPermitidos(SugestaoAporteResponseDto r)
    {
        var valores = new List<decimal>
        {
            r.AporteMensalNecessario, r.RendaMensal, r.DespesaMensalMedia, r.AportesMensaisMedios,
            r.SobraLivre, r.Deficit, r.FolgaRestante, r.CapacidadeMaxima,
        };

        valores.AddRange(r.Cortes.Select(c => c.ValorCorte));
        valores.AddRange(r.Cortes.Select(c => c.GastoAtual));
        valores.AddRange(r.Cortes.Select(c => c.GastoDepoisDoCorte));
        valores.AddRange(r.NaoClassificadas.Select(c => c.GastoMensalMedio));

        if (r.Reserva is not null)
            valores.AddRange(new[] { r.Reserva.ValorIdeal, r.Reserva.ValorAtual, r.Reserva.ValorFaltante });

        if (r.Cenarios is not null)
        {
            valores.Add(r.Cenarios.AporteSustentavel);
            if (r.Cenarios.ValorAlvoAlternativo.HasValue)
                valores.Add(r.Cenarios.ValorAlvoAlternativo.Value);
        }

        return valores.Select(v => Math.Round(v, 2)).ToHashSet();
    }

    /// <summary>
    /// Verifica que todo valor em reais citado pelo texto veio do bloco de fatos.
    /// É a trava contra o modelo "arredondar" ou inventar um número — a IA parafraseia,
    /// não calcula.
    /// </summary>
    private static bool SomenteValoresConhecidos(string texto, IReadOnlyCollection<decimal> permitidos)
    {
        foreach (Match match in ValorMonetario.Matches(texto))
        {
            var bruto = match.Groups[1].Value.TrimEnd('.', ',');
            if (!decimal.TryParse(bruto, NumberStyles.Currency, CulturaBrasil, out var valor))
                return false;

            if (!permitidos.Any(p => Math.Abs(p - valor) < 0.01m))
                return false;
        }

        return true;
    }

    /// <summary>Texto determinístico usado sempre que a IA não está disponível ou não é confiável.</summary>
    private static string MontarTextoTemplate(SugestaoAporteResponseDto r)
    {
        var texto = new StringBuilder();

        texto.Append($"Com renda de {Moeda(r.RendaMensal)} e despesas médias de {Moeda(r.DespesaMensalMedia)} por mês, ");
        texto.Append($"sobram {Moeda(r.SobraLivre)} livres. ");

        if (r.Cabe && r.Cortes.Count == 0)
        {
            texto.Append($"O aporte de {Moeda(r.AporteMensalNecessario)} cabe no seu orçamento e ainda deixa ");
            texto.Append($"{Moeda(r.FolgaRestante)} de folga — ele consome {r.PercentualDaRenda:0.##}% da sua renda.");
        }
        else if (r.Cabe)
        {
            texto.Append($"O aporte de {Moeda(r.AporteMensalNecessario)} só cabe com ajuste: ");
            texto.Append($"faltam {Moeda(r.Deficit)} por mês, que saem dos cortes abaixo.");
        }
        else
        {
            texto.Append($"O aporte de {Moeda(r.AporteMensalNecessario)} não cabe: faltam {Moeda(r.Deficit)} por mês ");
            texto.Append($"e o máximo que você sustenta hoje é {Moeda(r.CapacidadeMaxima)}. ");
            texto.Append("Vale rever o prazo ou o valor-alvo — veja os cenários abaixo.");
        }

        if (r.Reserva is { Adequada: false })
        {
            texto.Append($" Antes da meta, considere completar a reserva de emergência: faltam {Moeda(r.Reserva.ValorFaltante)}.");
        }

        if (r.NaoClassificadas.Count > 0)
        {
            texto.Append($" Há {r.NaoClassificadas.Count} categoria(s) sem classificação; ");
            texto.Append("marque quais são essenciais para eu apontar de onde cortar.");
        }

        return texto.ToString();
    }

    private static string Moeda(decimal valor) => valor.ToString("C2", CulturaBrasil);

    private static ServiceResponse<SugestaoAporteResponseDto> Falha(string mensagem) => new()
    {
        Success = false,
        ErrorMessage = mensagem,
    };
}
