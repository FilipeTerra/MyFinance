using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class ProjecaoInvestimentoCalculatorTests
{
    [Fact]
    public void Calcular_WithKnownValues_MatchesClosedFormFormula()
    {
        // Mesmo caso do teste equivalente em Python (tools.py: simular_investimento):
        // C=10000, aporte=500/mês, 12% a.a., 60 meses -> ~57794.05
        var resultado = ProjecaoInvestimentoCalculator.Calcular(10000m, 500m, 12.0m, 60);

        Assert.Equal(57794.05m, resultado.ValorFinal, 1);
        Assert.Equal(10000m + 500m * 60, resultado.TotalAportado);
        Assert.Equal(resultado.ValorFinal - resultado.TotalAportado, resultado.TotalJuros, 2);
    }

    [Fact]
    public void Calcular_WithoutMonthlyContribution_GrowsOnlyInitialCapital()
    {
        var resultado = ProjecaoInvestimentoCalculator.Calcular(1000m, 0m, 12.0m, 12);

        Assert.Equal(1120.0m, resultado.ValorFinal, 1);
        Assert.Equal(1000m, resultado.TotalAportado);
    }

    [Fact]
    public void Calcular_WithZeroRate_GrowthIsPurelyLinear()
    {
        var resultado = ProjecaoInvestimentoCalculator.Calcular(0m, 1000m, 0m, 120);

        Assert.Equal(1000m * 120, resultado.ValorFinal);
        Assert.Equal(1000m * 120, resultado.TotalAportado);
        Assert.Equal(0m, resultado.TotalJuros);
    }

    [Fact]
    public void Calcular_GeneratesOneEvolucaoEntryPerMonth()
    {
        var resultado = ProjecaoInvestimentoCalculator.Calcular(0m, 1000m, 10.75m, 120);

        Assert.Equal(120, resultado.Evolucao.Count);
        Assert.Equal(1, resultado.Evolucao[0].Mes);
        Assert.Equal(120, resultado.Evolucao[^1].Mes);
        Assert.Equal(1000m * 120, resultado.Evolucao[^1].TotalAportadoAcumulado);
    }

    [Fact]
    public void Calcular_RentabilidadePercentual_MatchesTotalJurosOverTotalAportado()
    {
        var resultado = ProjecaoInvestimentoCalculator.Calcular(10000m, 500m, 12.0m, 60);

        var esperado = Math.Round(resultado.TotalJuros / resultado.TotalAportado * 100, 2);
        Assert.Equal(esperado, resultado.RentabilidadePercentual);
    }

    [Theory]
    [InlineData(-1, 0, 10, 12)]
    [InlineData(0, -1, 10, 12)]
    [InlineData(0, 0, -1, 12)]
    [InlineData(0, 0, 10, 0)]
    [InlineData(0, 0, 10, -1)]
    public void Calcular_WithInvalidArguments_ThrowsArgumentException(
        decimal aporteInicial, decimal aporteMensal, decimal taxaJurosAnualPercentual, int meses)
    {
        Assert.Throws<ArgumentException>(() =>
            ProjecaoInvestimentoCalculator.Calcular(aporteInicial, aporteMensal, taxaJurosAnualPercentual, meses));
    }

    // ---------- Aportes extras ----------

    [Fact]
    public void Calcular_WithAporteExtra_AddsToAporteInTheGivenMonth()
    {
        var semExtra = ProjecaoInvestimentoCalculator.Calcular(0m, 100m, 0m, 3);
        var comExtra = ProjecaoInvestimentoCalculator.Calcular(0m, 100m, 0m, 3, new[] { new AporteExtra(2, 5000m) });

        Assert.Equal(semExtra.TotalAportado + 5000m, comExtra.TotalAportado);
        Assert.Equal(semExtra.Evolucao[0].ValorAcumulado, comExtra.Evolucao[0].ValorAcumulado);
        Assert.Equal(semExtra.Evolucao[1].ValorAcumulado + 5000m, comExtra.Evolucao[1].ValorAcumulado);
    }

    [Fact]
    public void Calcular_WithMultipleAportesExtrasSameMonth_SumsThem()
    {
        var resultado = ProjecaoInvestimentoCalculator.Calcular(
            0m, 100m, 0m, 2, new[] { new AporteExtra(1, 1000m), new AporteExtra(1, 500m) });

        Assert.Equal(100m + 1500m + 100m, resultado.TotalAportado);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Calcular_WithInvalidAporteExtra_ThrowsArgumentException(int mes, decimal valor)
    {
        Assert.Throws<ArgumentException>(() =>
            ProjecaoInvestimentoCalculator.Calcular(0m, 100m, 10m, 12, new[] { new AporteExtra(mes, valor) }));
    }

    // ---------- Reajuste anual do aporte ----------

    [Fact]
    public void Calcular_WithReajusteAnual_IncreasesAporteEveryTwelveMonths()
    {
        var resultado = ProjecaoInvestimentoCalculator.Calcular(0m, 100m, 0m, 25, reajusteAnualPercentual: 10m);

        Assert.Equal(100m, resultado.Evolucao[11].ValorAcumulado - resultado.Evolucao[10].ValorAcumulado);
        Assert.Equal(110m, resultado.Evolucao[12].ValorAcumulado - resultado.Evolucao[11].ValorAcumulado);
        Assert.Equal(121m, resultado.Evolucao[24].ValorAcumulado - resultado.Evolucao[23].ValorAcumulado);
    }

    [Fact]
    public void Calcular_WithZeroReajuste_BehavesLikeConstanteAporte()
    {
        var constante = ProjecaoInvestimentoCalculator.Calcular(0m, 100m, 12m, 24);
        var comReajusteZero = ProjecaoInvestimentoCalculator.Calcular(0m, 100m, 12m, 24, reajusteAnualPercentual: 0m);

        Assert.Equal(constante.ValorFinal, comReajusteZero.ValorFinal);
    }

    [Fact]
    public void Calcular_WithNegativeReajuste_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ProjecaoInvestimentoCalculator.Calcular(0m, 100m, 10m, 12, reajusteAnualPercentual: -1m));
    }
}
