using Moq;
using MyFinance.Application.Dtos.Analytics;
using MyFinance.Application.Dtos.Sugestao;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Tests.Services;

public class SugestaoAporteServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAnalyticsRepository> _analyticsRepository = new();
    private readonly Mock<IFinancialGoalRepository> _goalRepository = new();
    private readonly Mock<IInvestimentoRepository> _investimentoRepository = new();
    private readonly Mock<ITaxasReferenciaIntegrationService> _taxasReferenciaService = new();
    private readonly Mock<IAiIntegrationService> _aiIntegrationService = new();
    private readonly SugestaoAporteService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    private static readonly DateTime MesPassado = DateTime.UtcNow.AddMonths(-1);

    public SugestaoAporteServiceTests()
    {
        var projecaoService = new ProjecaoInvestimentoService(_taxasReferenciaService.Object);
        var metaReversaService = new MetaReversaService(projecaoService, _goalRepository.Object);

        _userRepository.Setup(r => r.GetUserByIdAsync(_userId))
            .ReturnsAsync(new User { Id = _userId, MonthlyIncome = 5000m });
        _goalRepository.Setup(r => r.GetAllByUserIdAsync(_userId))
            .ReturnsAsync(Array.Empty<FinancialGoal>());
        _investimentoRepository.Setup(r => r.GetAllByUserIdAsync(_userId))
            .ReturnsAsync(Array.Empty<Investimento>());

        SetAnalytics();

        // Por padrão a IA não responde: o caminho determinístico é o normal, não a exceção.
        _aiIntegrationService.Setup(a => a.NarrateSuggestionAsync(It.IsAny<SuggestionFactsDto>()))
            .ReturnsAsync((string?)null);

        _sut = new SugestaoAporteService(
            _userRepository.Object,
            _analyticsRepository.Object,
            _goalRepository.Object,
            _investimentoRepository.Object,
            metaReversaService,
            projecaoService,
            _aiIntegrationService.Object);
    }

    private void SetAnalytics(
        IEnumerable<MonthlyCategoryTotalDto>? gastos = null,
        IEnumerable<MonthlyFlowDto>? fluxo = null,
        IEnumerable<MonthlyInvestmentTotalDto>? aportes = null)
    {
        _analyticsRepository
            .Setup(r => r.GetMonthlyCategoryTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(gastos ?? Array.Empty<MonthlyCategoryTotalDto>());
        _analyticsRepository
            .Setup(r => r.GetMonthlyFlowAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(fluxo ?? Array.Empty<MonthlyFlowDto>());
        _analyticsRepository
            .Setup(r => r.GetMonthlyInvestmentTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(aportes ?? Array.Empty<MonthlyInvestmentTotalDto>());
    }

    private static MonthlyCategoryTotalDto Gasto(string nome, decimal total, ExpenseNature nature, DateTime? quando = null)
    {
        var data = quando ?? MesPassado;
        return new MonthlyCategoryTotalDto
        {
            Year = data.Year,
            Month = data.Month,
            CategoryId = Guid.NewGuid(),
            CategoryName = nome,
            Total = total,
            Nature = nature,
        };
    }

    private static SugestaoAporteRequestDto Request(decimal? aporte = 500m, decimal alvo = 50000m, int prazo = 60) => new()
    {
        AporteInicial = 0m,
        PrazoMeses = prazo,
        ValorAlvo = alvo,
        AporteMensalNecessario = aporte,
        FonteTaxaJuros = FonteTaxaJuros.Manual,
        TaxaJurosAnualPercentual = 10m,
        TipoAtivo = TipoAtivoCalculadora.Lci,
    };

    [Fact]
    public async Task ObterSugestaoAsync_WithDeclaredSalary_UsesProfileAsIncomeSource()
    {
        var response = await _sut.ObterSugestaoAsync(_userId, Request());

        Assert.True(response.Success);
        Assert.Equal(FonteRenda.PerfilDeclarado, response.Data!.FonteRenda);
        Assert.Equal(5000m, response.Data.RendaMensal);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WithoutDeclaredSalary_FallsBackToRealizedIncome()
    {
        _userRepository.Setup(r => r.GetUserByIdAsync(_userId))
            .ReturnsAsync(new User { Id = _userId, MonthlyIncome = null });
        SetAnalytics(
            gastos: new[] { Gasto("Aluguel", 1000m, ExpenseNature.Essencial) },
            fluxo: new[]
            {
                new MonthlyFlowDto { Year = MesPassado.Year, Month = MesPassado.Month, TotalIncome = 3000m, TotalExpenses = 1000m },
            });

        var response = await _sut.ObterSugestaoAsync(_userId, Request());

        Assert.True(response.Success);
        Assert.Equal(FonteRenda.TransacoesRealizadas, response.Data!.FonteRenda);
        Assert.Equal(3000m, response.Data.RendaMensal);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WithoutAnyIncome_FailsWithActionableMessage()
    {
        _userRepository.Setup(r => r.GetUserByIdAsync(_userId))
            .ReturnsAsync(new User { Id = _userId, MonthlyIncome = null });

        var response = await _sut.ObterSugestaoAsync(_userId, Request());

        Assert.False(response.Success);
        Assert.Contains("salário", response.ErrorMessage);
    }

    [Fact]
    public async Task ObterSugestaoAsync_AveragesExpensesOverMonthsWithData()
    {
        var doisMesesAtras = DateTime.UtcNow.AddMonths(-2);
        SetAnalytics(gastos: new[]
        {
            Gasto("Aluguel", 1000m, ExpenseNature.Essencial, MesPassado),
            Gasto("Aluguel", 2000m, ExpenseNature.Essencial, doisMesesAtras),
        });

        var response = await _sut.ObterSugestaoAsync(_userId, Request());

        // Dois meses com dado: 3000 no total vira 1500 de média mensal.
        Assert.Equal(2, response.Data!.MesesAnalisados);
        Assert.Equal(1500m, response.Data.DespesaMensalMedia);
        Assert.False(response.Data.HistoricoInsuficiente);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WithSingleMonthOfData_FlagsInsufficientHistory()
    {
        SetAnalytics(gastos: new[] { Gasto("Aluguel", 1000m, ExpenseNature.Essencial) });

        var response = await _sut.ObterSugestaoAsync(_userId, Request());

        Assert.Equal(1, response.Data!.MesesAnalisados);
        Assert.True(response.Data.HistoricoInsuficiente);
    }

    [Fact]
    public async Task ObterSugestaoAsync_AnalysisWindowExcludesCurrentMonth()
    {
        DateTime? fimCapturado = null;
        _analyticsRepository
            .Setup(r => r.GetMonthlyCategoryTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .Callback<Guid, DateTime, DateTime, Guid?>((_, _, fim, _) => fimCapturado = fim)
            .ReturnsAsync(Array.Empty<MonthlyCategoryTotalDto>());

        await _sut.ObterSugestaoAsync(_userId, Request());

        // O período fecha no último dia do mês anterior: meio mês lançado inflaria a sobra.
        var ultimoDiaDoMesAnterior = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddDays(-1);
        Assert.Equal(ultimoDiaDoMesAnterior.Date, fimCapturado!.Value.Date);
    }

    [Fact]
    public async Task ObterSugestaoAsync_ExistingContributionsReduceFreeSurplus()
    {
        SetAnalytics(
            gastos: new[] { Gasto("Aluguel", 2000m, ExpenseNature.Essencial) },
            aportes: new[]
            {
                new MonthlyInvestmentTotalDto { Year = MesPassado.Year, Month = MesPassado.Month, Total = 800m },
            });

        var response = await _sut.ObterSugestaoAsync(_userId, Request());

        Assert.Equal(800m, response.Data!.AportesMensaisMedios);
        Assert.Equal(2200m, response.Data.SobraLivre);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WhenAporteDoesNotFit_ReturnsAlternativeScenarios()
    {
        SetAnalytics(gastos: new[] { Gasto("Aluguel", 4900m, ExpenseNature.Essencial) });

        // Sobra livre de 100 contra um aporte de 2000: não cabe de jeito nenhum.
        var response = await _sut.ObterSugestaoAsync(_userId, Request(aporte: 2000m));

        Assert.False(response.Data!.Cabe);
        Assert.NotNull(response.Data.Cenarios);
        Assert.Equal(100m, response.Data.Cenarios!.AporteSustentavel);
        Assert.NotNull(response.Data.Cenarios.ValorAlvoAlternativo);
        Assert.True(response.Data.Cenarios.ValorAlvoAlternativo < response.Data.AporteMensalNecessario * 60);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WhenAporteFits_DoesNotComputeAlternativeScenarios()
    {
        SetAnalytics(gastos: new[] { Gasto("Aluguel", 1000m, ExpenseNature.Essencial) });

        var response = await _sut.ObterSugestaoAsync(_userId, Request(aporte: 500m));

        Assert.True(response.Data!.Cabe);
        Assert.Null(response.Data.Cenarios);
    }

    [Fact]
    public async Task ObterSugestaoAsync_ReportsIncompleteEmergencyReserve()
    {
        var meta = new FinancialGoal(_userId, "Reserva de emergência", 30000m, DateTime.UtcNow.AddYears(1));
        meta.AddFunds(6000m);
        _goalRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(new[] { meta });

        var response = await _sut.ObterSugestaoAsync(_userId, Request());

        Assert.NotNull(response.Data!.Reserva);
        Assert.False(response.Data.Reserva!.Adequada);
        Assert.Equal(30000m, response.Data.Reserva.ValorIdeal);
        Assert.Equal(6000m, response.Data.Reserva.ValorAtual);
        Assert.Equal(24000m, response.Data.Reserva.ValorFaltante);
        Assert.Contains("reserva de emergência", response.Data.TextoConsultivo);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WhenAiIsUnavailable_UsesDeterministicTemplate()
    {
        var response = await _sut.ObterSugestaoAsync(_userId, Request());

        Assert.True(response.Success);
        Assert.False(response.Data!.IaUsada);
        Assert.True(response.Data.IaIndisponivel);
        Assert.NotEmpty(response.Data.TextoConsultivo);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WhenAiOnlyCitesKnownAmounts_UsesAiText()
    {
        _aiIntegrationService.Setup(a => a.NarrateSuggestionAsync(It.IsAny<SuggestionFactsDto>()))
            .ReturnsAsync("Sua renda de R$ 5.000,00 comporta o aporte de R$ 500,00 com folga.");

        var response = await _sut.ObterSugestaoAsync(_userId, Request(aporte: 500m));

        Assert.True(response.Data!.IaUsada);
        Assert.False(response.Data.IaIndisponivel);
        Assert.StartsWith("Sua renda", response.Data.TextoConsultivo);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WhenAiInventsAnAmount_FallsBackToTemplate()
    {
        // R$ 9.999,00 nunca foi enviado como fato: a resposta inteira é descartada.
        _aiIntegrationService.Setup(a => a.NarrateSuggestionAsync(It.IsAny<SuggestionFactsDto>()))
            .ReturnsAsync("Você consegue guardar R$ 9.999,00 por mês tranquilamente.");

        var response = await _sut.ObterSugestaoAsync(_userId, Request(aporte: 500m));

        Assert.False(response.Data!.IaUsada);
        Assert.True(response.Data.IaIndisponivel);
        Assert.DoesNotContain("9.999", response.Data.TextoConsultivo);
    }

    [Fact]
    public async Task ObterSugestaoAsync_AiMayCiteNonMonetaryNumbers()
    {
        _aiIntegrationService.Setup(a => a.NarrateSuggestionAsync(It.IsAny<SuggestionFactsDto>()))
            .ReturnsAsync("Em 6 meses, com 10% ao ano, o aporte de R$ 500,00 se acumula.");

        var response = await _sut.ObterSugestaoAsync(_userId, Request(aporte: 500m));

        Assert.True(response.Data!.IaUsada);
    }

    [Fact]
    public async Task ObterSugestaoAsync_NeverCutsUnclassifiedCategoriesButListsThem()
    {
        SetAnalytics(gastos: new[]
        {
            Gasto("Aluguel", 3000m, ExpenseNature.Essencial),
            Gasto("Mercado", 1500m, ExpenseNature.NaoClassificado),
            Gasto("Diversos", 400m, ExpenseNature.NaoClassificado),
        });

        var response = await _sut.ObterSugestaoAsync(_userId, Request(aporte: 1000m));

        Assert.Empty(response.Data!.Cortes);
        Assert.Equal(new[] { "Mercado", "Diversos" }, response.Data.NaoClassificadas.Select(c => c.CategoryName));
        Assert.Contains("sem classificação", response.Data.TextoConsultivo);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WhenAporteIsOmitted_ComputesItFromTheGoal()
    {
        SetAnalytics(gastos: new[] { Gasto("Aluguel", 1000m, ExpenseNature.Essencial) });

        var response = await _sut.ObterSugestaoAsync(_userId, Request(aporte: null, alvo: 50000m, prazo: 60));

        Assert.True(response.Success);
        Assert.True(response.Data!.AporteMensalNecessario > 0);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(50000, 0)]
    public async Task ObterSugestaoAsync_WithInvalidGoalParameters_Fails(decimal alvo, int prazo)
    {
        var response = await _sut.ObterSugestaoAsync(_userId, Request(alvo: alvo, prazo: prazo));

        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
    }

    [Fact]
    public async Task ObterSugestaoAsync_WithUnknownUser_Fails()
    {
        _userRepository.Setup(r => r.GetUserByIdAsync(_userId)).ReturnsAsync((User?)null);

        var response = await _sut.ObterSugestaoAsync(_userId, Request());

        Assert.False(response.Success);
        Assert.Contains("não encontrado", response.ErrorMessage);
    }
}
