using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;
using static MyFinance.Domain.Services.SugestaoAporteCalculator;

namespace MyFinance.Domain.Tests.Services;

public class SugestaoAporteCalculatorTests
{
    private static GastoCategoria Gasto(string nome, decimal media, ExpenseNature nature) =>
        new(Guid.NewGuid(), nome, media, nature);

    private static PerfilFinanceiroMensal Perfil(
        decimal renda, decimal aportes = 0m, params GastoCategoria[] gastos) =>
        new(renda, gastos, aportes);

    [Fact]
    public void Calcular_WhenAporteFitsInSurplus_ReturnsCabeWithRemainingSlack()
    {
        var perfil = Perfil(5000m, 0m,
            Gasto("Aluguel", 2000m, ExpenseNature.Essencial),
            Gasto("Lazer", 500m, ExpenseNature.Discricionario));

        var resultado = SugestaoAporteCalculator.Calcular(perfil, 1000m);

        Assert.True(resultado.Cabe);
        Assert.Equal(2500m, resultado.SobraLivre);
        Assert.Equal(0m, resultado.Deficit);
        Assert.Equal(1500m, resultado.FolgaRestante);
        Assert.Empty(resultado.Cortes);
        Assert.Equal(20m, resultado.PercentualDaRenda);
    }

    [Fact]
    public void Calcular_WithExistingContributions_ReducesFreeSurplus()
    {
        var perfil = Perfil(5000m, 800m, Gasto("Aluguel", 2000m, ExpenseNature.Essencial));

        var resultado = SugestaoAporteCalculator.Calcular(perfil, 1000m);

        // Sobra total ignora o que já é aportado; sobra livre desconta.
        Assert.Equal(3000m, resultado.SobraTotal);
        Assert.Equal(2200m, resultado.SobraLivre);
        Assert.True(resultado.Cabe);
    }

    [Fact]
    public void Calcular_WhenAporteExceedsSurplus_SpreadsCutAcrossDiscretionaryCategories()
    {
        var perfil = Perfil(4000m, 0m,
            Gasto("Aluguel", 3000m, ExpenseNature.Essencial),
            Gasto("Delivery", 600m, ExpenseNature.Discricionario),
            Gasto("Streaming", 200m, ExpenseNature.Discricionario));

        // Sobra livre = 200; aporte 400 => déficit 200.
        var resultado = SugestaoAporteCalculator.Calcular(perfil, 400m);

        Assert.True(resultado.Cabe);
        Assert.Equal(200m, resultado.Deficit);
        Assert.Equal(200m, resultado.Cortes.Sum(c => c.ValorCorte));

        // Rateio proporcional ao peso: Delivery gasta 3x mais que Streaming.
        var delivery = resultado.Cortes.Single(c => c.Nome == "Delivery");
        var streaming = resultado.Cortes.Single(c => c.Nome == "Streaming");
        Assert.Equal(150m, delivery.ValorCorte);
        Assert.Equal(50m, streaming.ValorCorte);
        Assert.Equal(450m, delivery.GastoDepoisDoCorte);
    }

    [Fact]
    public void Calcular_NeverCutsMoreThanCeilingPerCategory()
    {
        var perfil = Perfil(3000m, 0m,
            Gasto("Aluguel", 2500m, ExpenseNature.Essencial),
            Gasto("Lazer", 500m, ExpenseNature.Discricionario));

        // Sobra livre = 0; aporte 1000 => déficit maior que o teto de 40% de Lazer (200).
        var resultado = SugestaoAporteCalculator.Calcular(perfil, 1000m);

        var lazer = Assert.Single(resultado.Cortes);
        Assert.Equal(200m, lazer.ValorCorte);
        Assert.False(resultado.Cabe);
    }

    [Fact]
    public void Calcular_NeverCutsEssentialCategories()
    {
        var perfil = Perfil(3000m, 0m, Gasto("Aluguel", 3000m, ExpenseNature.Essencial));

        var resultado = SugestaoAporteCalculator.Calcular(perfil, 500m);

        Assert.Empty(resultado.Cortes);
        Assert.False(resultado.Cabe);
    }

    [Fact]
    public void Calcular_NeverCutsUnclassifiedCategoriesButReportsThem()
    {
        var perfil = Perfil(3000m, 0m,
            Gasto("Aluguel", 2000m, ExpenseNature.Essencial),
            Gasto("Mercado", 600m, ExpenseNature.NaoClassificado),
            Gasto("Diversos", 400m, ExpenseNature.NaoClassificado));

        var resultado = SugestaoAporteCalculator.Calcular(perfil, 500m);

        Assert.Empty(resultado.Cortes);
        Assert.False(resultado.Cabe);

        // Voltam ordenadas por gasto, para a UI pedir a classificação do que mais importa.
        Assert.Equal(new[] { "Mercado", "Diversos" }, resultado.NaoClassificadas.Select(c => c.Nome));
    }

    [Fact]
    public void Calcular_WhenDeficitExceedsAllPossibleCuts_ReportsMaximumCapacity()
    {
        var perfil = Perfil(4000m, 0m,
            Gasto("Aluguel", 3500m, ExpenseNature.Essencial),
            Gasto("Lazer", 500m, ExpenseNature.Discricionario));

        // Sobra livre = 0; corte máximo = 40% de 500 = 200. Aporte de 5000 é impossível.
        var resultado = SugestaoAporteCalculator.Calcular(perfil, 5000m);

        Assert.False(resultado.Cabe);
        Assert.Equal(5000m, resultado.Deficit);
        Assert.Equal(200m, resultado.CapacidadeMaxima);
        Assert.Equal(200m, resultado.Cortes.Sum(c => c.ValorCorte));
    }

    [Fact]
    public void Calcular_WithNegativeSurplus_StillReportsCapacityFromCuts()
    {
        var perfil = Perfil(2000m, 0m,
            Gasto("Aluguel", 1800m, ExpenseNature.Essencial),
            Gasto("Lazer", 400m, ExpenseNature.Discricionario));

        // O usuário já gasta mais do que ganha: sobra livre = -200.
        var resultado = SugestaoAporteCalculator.Calcular(perfil, 100m);

        Assert.False(resultado.Cabe);
        Assert.Equal(-200m, resultado.SobraLivre);
        Assert.Equal(-40m, resultado.CapacidadeMaxima);
    }

    [Fact]
    public void Calcular_WithoutExpenses_UsesEntireIncomeAsSurplus()
    {
        var resultado = SugestaoAporteCalculator.Calcular(Perfil(3000m), 1000m);

        Assert.True(resultado.Cabe);
        Assert.Equal(0m, resultado.DespesaTotal);
        Assert.Equal(3000m, resultado.SobraLivre);
        Assert.Empty(resultado.NaoClassificadas);
    }

    [Fact]
    public void Calcular_WithZeroIncome_DoesNotDivideByZero()
    {
        var resultado = SugestaoAporteCalculator.Calcular(Perfil(0m), 500m);

        Assert.False(resultado.Cabe);
        Assert.Equal(0m, resultado.PercentualDaRenda);
        Assert.Equal(500m, resultado.Deficit);
    }

    [Fact]
    public void Calcular_WithZeroAporte_AlwaysFits()
    {
        var resultado = SugestaoAporteCalculator.Calcular(Perfil(1000m), 0m);

        Assert.True(resultado.Cabe);
        Assert.Equal(0m, resultado.PercentualDaRenda);
    }

    [Fact]
    public void Calcular_WithNegativeIncome_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SugestaoAporteCalculator.Calcular(Perfil(-1m), 100m));
    }

    [Fact]
    public void Calcular_WithNegativeAporte_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SugestaoAporteCalculator.Calcular(Perfil(1000m), -1m));
    }
}
