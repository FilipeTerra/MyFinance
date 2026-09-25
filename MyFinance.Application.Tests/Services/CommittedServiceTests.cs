using Moq;
using MyFinance.Application.Dtos.Analytics;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;

namespace MyFinance.Application.Tests.Services;

public class CommittedServiceTests
{
    private readonly Mock<IAnalyticsRepository> _analyticsRepository = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _categoriaId = Guid.NewGuid();

    private CommittedService BuildSut() => new(_analyticsRepository.Object);

    private void Retorna(params InstallmentRowDto[] rows) =>
        _analyticsRepository
            .Setup(r => r.GetInstallmentsAsync(_userId, It.IsAny<Guid?>(), It.IsAny<DateTime>()))
            .ReturnsAsync(rows);

    /// <summary>Uma parcela lançada há <paramref name="mesesAtras"/> meses.</summary>
    private InstallmentRowDto Row(
        string descricao, decimal valor, int numero, int total, int mesesAtras = 0, Guid? accountId = null) =>
        new()
        {
            Description = descricao,
            Amount = valor,
            Date = DateTime.UtcNow.Date.AddMonths(-mesesAtras),
            AccountId = accountId ?? _accountId,
            InstallmentNumber = numero,
            InstallmentTotal = total,
            CategoryId = _categoriaId,
            CategoryName = "Compras",
        };

    [Fact]
    public async Task GetCommittedAsync_SemParcelamento_DevolveZerado()
    {
        Retorna();

        var response = await BuildSut().GetCommittedAsync(_userId, null);

        Assert.True(response.Success);
        Assert.Equal(0m, response.Data!.TotalCommitted);
        Assert.Empty(response.Data.Purchases);
        Assert.Null(response.Data.LastDueMonth);
    }

    [Fact]
    public async Task GetCommittedAsync_AgrupaParcelasDaMesmaCompraApesarDoSufixoNaDescricao()
    {
        // A descrição muda de uma fatura para a outra só no número da parcela.
        Retorna(
            Row("CENTAURO.COM (Parcela 01 de 04)", 100m, 1, 4, mesesAtras: 2),
            Row("CENTAURO.COM (Parcela 02 de 04)", 100m, 2, 4, mesesAtras: 1),
            Row("CENTAURO.COM (Parcela 03 de 04)", 100m, 3, 4));

        var response = await BuildSut().GetCommittedAsync(_userId, null);

        var compra = Assert.Single(response.Data!.Purchases);
        Assert.Equal(3, compra.CurrentInstallment);
        Assert.Equal(1, compra.RemainingInstallments);
        Assert.Equal(100m, response.Data.TotalCommitted);
    }

    [Fact]
    public async Task GetCommittedAsync_TiraOSufixoDeParcelaDaDescricaoQueVaiParaATela()
    {
        Retorna(Row("CP PARC DUO GOURMET (Parcela 06 de 09)", 59.78m, 6, 9));

        var response = await BuildSut().GetCommittedAsync(_userId, null);

        Assert.Equal("CP PARC DUO GOURMET", Assert.Single(response.Data!.Purchases).Description);
    }

    [Fact]
    public async Task GetCommittedAsync_MesmaLojaComValoresDiferentes_SaoComprasDistintas()
    {
        // Duas compras na mesma loja, ambas em 4x, mas de valores diferentes: o valor da
        // parcela faz parte da identidade justamente para não fundir as duas.
        Retorna(
            Row("CENTAURO.COM", 100m, 2, 4),
            Row("CENTAURO.COM", 250m, 1, 4));

        var response = await BuildSut().GetCommittedAsync(_userId, null);

        Assert.Equal(2, response.Data!.PurchaseCount);
        Assert.Equal(950m, response.Data.TotalCommitted);
    }

    [Fact]
    public async Task GetCommittedAsync_MesmaCompraEmContasDiferentes_NaoEhFundida()
    {
        Retorna(
            Row("CENTAURO.COM", 100m, 2, 4),
            Row("CENTAURO.COM", 100m, 2, 4, accountId: Guid.NewGuid()));

        var response = await BuildSut().GetCommittedAsync(_userId, null);

        Assert.Equal(2, response.Data!.PurchaseCount);
    }

    [Fact]
    public async Task GetCommittedAsync_FormataOsMesesComoAaaaMm()
    {
        Retorna(Row("LOJA", 100m, 1, 3));

        var response = await BuildSut().GetCommittedAsync(_userId, null);

        var compra = Assert.Single(response.Data!.Purchases);
        Assert.Matches(@"^\d{4}-\d{2}$", compra.LastDueMonth);
        Assert.Equal(compra.LastDueMonth, response.Data.LastDueMonth);
        Assert.All(response.Data.Schedule, m => Assert.Matches(@"^\d{4}-\d{2}$", m.Month));
    }

    [Fact]
    public async Task GetCommittedAsync_LastDueMonth_EhODaCompraQueTerminaPorUltimo()
    {
        Retorna(
            Row("CURTA", 100m, 1, 2),
            Row("LONGA", 50m, 1, 10));

        var response = await BuildSut().GetCommittedAsync(_userId, null);

        var maisLonga = response.Data!.Purchases.Single(p => p.Description == "LONGA");
        Assert.Equal(maisLonga.LastDueMonth, response.Data.LastDueMonth);
    }

    [Fact]
    public async Task GetCommittedAsync_RepassaAContaEscolhidaParaORepositorio()
    {
        Retorna();

        await BuildSut().GetCommittedAsync(_userId, _accountId);

        _analyticsRepository.Verify(
            r => r.GetInstallmentsAsync(_userId, _accountId, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task GetCommittedAsync_LimitaAJanelaDeBuscaAoHistoricoRelevante()
    {
        Retorna();

        await BuildSut().GetCommittedAsync(_userId, null);

        _analyticsRepository.Verify(
            r => r.GetInstallmentsAsync(_userId, null, It.Is<DateTime>(d => d < DateTime.UtcNow.Date)),
            Times.Once);
    }
}
