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
}
