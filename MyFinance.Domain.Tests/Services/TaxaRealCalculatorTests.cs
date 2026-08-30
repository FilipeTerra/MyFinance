using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class TaxaRealCalculatorTests
{
    [Fact]
    public void Calcular_WithKnownValues_MatchesFisherEquation()
    {
        // (1.12 / 1.04 - 1) * 100 = 7.6923...
        var resultado = TaxaRealCalculator.Calcular(taxaNominalAnualPercentual: 12m, inflacaoAnualPercentual: 4m);

        Assert.Equal(7.69m, resultado, 2);
    }

    [Fact]
    public void Calcular_WithZeroInflation_ReturnsNominalRate()
    {
        var resultado = TaxaRealCalculator.Calcular(10m, 0m);

        Assert.Equal(10m, resultado);
    }

    [Fact]
    public void Calcular_WhenInflationExceedsNominal_ReturnsNegativeRealRate()
    {
        var resultado = TaxaRealCalculator.Calcular(taxaNominalAnualPercentual: 5m, inflacaoAnualPercentual: 8m);

        Assert.True(resultado < 0);
    }

    [Fact]
    public void Calcular_WithMinusOneHundredPercentInflation_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TaxaRealCalculator.Calcular(10m, -100m));
    }
}
