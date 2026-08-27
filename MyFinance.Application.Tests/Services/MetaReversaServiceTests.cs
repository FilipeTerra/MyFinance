using Moq;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Tests.Services;

public class MetaReversaServiceTests
{
    private readonly Mock<ITaxasReferenciaIntegrationService> _taxasReferenciaService = new();
    private readonly Mock<IFinancialGoalRepository> _goalRepository = new();
    private readonly IProjecaoInvestimentoService _projecaoService;
    private readonly MetaReversaService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public MetaReversaServiceTests()
    {
        _projecaoService = new ProjecaoInvestimentoService(_taxasReferenciaService.Object);
        _sut = new MetaReversaService(_projecaoService, _goalRepository.Object);
    }

    // ---------- CalcularAporteNecessarioAsync ----------

    [Fact]
    public async Task CalcularAporteNecessarioAsync_WithTargetAlreadyMet_ReturnsZero()
    {
        var request = new CalcularAporteNecessarioRequestDto
        {
            AporteInicial = 50000m,
            ValorAlvo = 10000m,
            PrazoMeses = 12,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.CalcularAporteNecessarioAsync(request);

        Assert.Equal(0m, result.AporteMensalNecessario);
        Assert.True(result.Projecao.ValorFinalLiquido >= request.ValorAlvo);
    }

    [Fact]
    public async Task CalcularAporteNecessarioAsync_ComputesAporteThatReachesTarget()
    {
        var request = new CalcularAporteNecessarioRequestDto
        {
            AporteInicial = 1000m,
            ValorAlvo = 50000m,
            PrazoMeses = 60,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 12m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.CalcularAporteNecessarioAsync(request);

        Assert.True(result.AporteMensalNecessario > 0);
        // Bate o alvo com a alíquota resolvida...
        Assert.True(result.Projecao.ValorFinalLiquido >= request.ValorAlvo - 1m);
        // ...e um aporte um pouco menor não bateria.
        var comMenosUmReal = await _projecaoService.CalcularProjecaoAsync(new CalcularProjecaoRequestDto
        {
            AporteInicial = request.AporteInicial,
            AporteMensal = result.AporteMensalNecessario - 5m,
            PrazoMeses = request.PrazoMeses,
            FonteTaxaJuros = request.FonteTaxaJuros,
            TaxaJurosAnualPercentual = request.TaxaJurosAnualPercentual,
            TipoAtivo = request.TipoAtivo
        });
        Assert.True(comMenosUmReal.ValorFinalLiquido < request.ValorAlvo);
    }

    [Fact]
    public async Task CalcularAporteNecessarioAsync_WithZeroTarget_ThrowsArgumentException()
    {
        var request = new CalcularAporteNecessarioRequestDto
        {
            AporteInicial = 1000m,
            ValorAlvo = 0m,
            PrazoMeses = 12,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CalcularAporteNecessarioAsync(request));
    }

    // ---------- CalcularPrazoNecessarioAsync ----------

    [Fact]
    public async Task CalcularPrazoNecessarioAsync_ComputesPrazoThatReachesTarget()
    {
        var request = new CalcularPrazoNecessarioRequestDto
        {
            AporteInicial = 0m,
            AporteMensal = 500m,
            ValorAlvo = 30000m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 12m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.CalcularPrazoNecessarioAsync(request);

        Assert.True(result.Atingivel);
        Assert.NotNull(result.PrazoMesesNecessario);
        Assert.True(result.Projecao!.ValorFinalLiquido >= request.ValorAlvo - 1m);

        var comUmMesAMenos = await _projecaoService.CalcularProjecaoAsync(new CalcularProjecaoRequestDto
        {
            AporteInicial = request.AporteInicial,
            AporteMensal = request.AporteMensal,
            PrazoMeses = result.PrazoMesesNecessario!.Value - 1,
            FonteTaxaJuros = request.FonteTaxaJuros,
            TaxaJurosAnualPercentual = request.TaxaJurosAnualPercentual,
            TipoAtivo = request.TipoAtivo
        });
        Assert.True(comUmMesAMenos.ValorFinalLiquido < request.ValorAlvo);
    }

    [Fact]
    public async Task CalcularPrazoNecessarioAsync_WhenUnreachable_ReturnsAtingivelFalse()
    {
        var request = new CalcularPrazoNecessarioRequestDto
        {
            AporteInicial = 0m,
            AporteMensal = 0m,
            ValorAlvo = 1000m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 0m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.CalcularPrazoNecessarioAsync(request);

        Assert.False(result.Atingivel);
        Assert.Null(result.PrazoMesesNecessario);
        Assert.Null(result.Projecao);
    }

    // ---------- SimularMetaAsync ----------

    [Fact]
    public async Task SimularMetaAsync_WithSufficientAporte_ReturnsAtingeTrue()
    {
        var deadline = DateTime.UtcNow.AddMonths(24);
        var goal = new FinancialGoal(_userId, "Carro", 20000m, deadline);
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);

        var request = new SimularMetaRequestDto
        {
            AporteMensal = 1000m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.SimularMetaAsync(goal.Id, _userId, request);

        Assert.Equal(24, result.PrazoMesesRestante);
        Assert.True(result.Atinge);
        Assert.True(result.DiferencaParaMeta >= 0);
        Assert.Null(result.AporteMensalNecessario);
    }

    [Fact]
    public async Task SimularMetaAsync_WithInsufficientAporte_ReturnsAtingeFalse()
    {
        var deadline = DateTime.UtcNow.AddMonths(6);
        var goal = new FinancialGoal(_userId, "Casa", 1000000m, deadline);
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);

        var request = new SimularMetaRequestDto
        {
            AporteMensal = 10m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.SimularMetaAsync(goal.Id, _userId, request);

        Assert.False(result.Atinge);
        Assert.True(result.DiferencaParaMeta < 0);
    }

    [Fact]
    public async Task SimularMetaAsync_WithoutAporte_ComputesAporteNecessario()
    {
        var deadline = DateTime.UtcNow.AddMonths(36);
        var goal = new FinancialGoal(_userId, "Aposentadoria", 50000m, deadline);
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);

        var request = new SimularMetaRequestDto
        {
            AporteMensal = null,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.SimularMetaAsync(goal.Id, _userId, request);

        Assert.True(result.Atinge);
        Assert.NotNull(result.AporteMensalNecessario);
        Assert.True(result.AporteMensalNecessario > 0);
        Assert.Equal(0m, result.DiferencaParaMeta);
    }

    [Fact]
    public async Task SimularMetaAsync_WithGoalFromAnotherUser_ThrowsUnauthorizedAccessException()
    {
        var goal = new FinancialGoal(Guid.NewGuid(), "Meta de outro usuário", 5000m, DateTime.UtcNow.AddMonths(12));
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);

        var request = new SimularMetaRequestDto { AporteMensal = 100m, FonteTaxaJuros = FonteTaxaJuros.Manual, TaxaJurosAnualPercentual = 10m, TipoAtivo = TipoAtivoCalculadora.Lci };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.SimularMetaAsync(goal.Id, _userId, request));
    }

    [Fact]
    public async Task SimularMetaAsync_WithGoalNotFound_ThrowsUnauthorizedAccessException()
    {
        _goalRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FinancialGoal?)null);

        var request = new SimularMetaRequestDto { AporteMensal = 100m, FonteTaxaJuros = FonteTaxaJuros.Manual, TaxaJurosAnualPercentual = 10m, TipoAtivo = TipoAtivoCalculadora.Lci };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.SimularMetaAsync(Guid.NewGuid(), _userId, request));
    }

    [Fact]
    public async Task SimularMetaAsync_WithPastDeadline_ThrowsInvalidOperationException()
    {
        var goal = new FinancialGoal(_userId, "Meta vencida", 5000m, DateTime.UtcNow.AddMonths(-2));
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);

        var request = new SimularMetaRequestDto { AporteMensal = 100m, FonteTaxaJuros = FonteTaxaJuros.Manual, TaxaJurosAnualPercentual = 10m, TipoAtivo = TipoAtivoCalculadora.Lci };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SimularMetaAsync(goal.Id, _userId, request));
    }
}
