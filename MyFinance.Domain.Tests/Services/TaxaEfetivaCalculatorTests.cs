using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class TaxaEfetivaCalculatorTests
{
    [Fact]
    public void Calcular_WithMonthlyCompounding_MatchesKnownEar()
    {
        // (1 + 0.12/12)^12 - 1 = 12.6825...%
        var resultado = TaxaEfetivaCalculator.Calcular(taxaNominalAnualPercentual: 12m, capitalizacoesPorAno: 12);

        Assert.Equal(12.68m, resultado, 2);
    }

    [Fact]
    public void Calcular_WithSingleCompoundingPerYear_EqualsNominalRate()
    {
        var resultado = TaxaEfetivaCalculator.Calcular(10m, 1);

        Assert.Equal(10m, resultado);
    }

    [Fact]
    public void Calcular_WithZeroRate_ReturnsZero()
    {
        var resultado = TaxaEfetivaCalculator.Calcular(0m, 12);

        Assert.Equal(0m, resultado);
    }

    [Theory]
    [InlineData(-1, 12)]
    [InlineData(10, 0)]
    [InlineData(10, -1)]
    public void Calcular_WithInvalidArguments_ThrowsArgumentException(decimal taxaNominal, int capitalizacoes)
    {
        Assert.Throws<ArgumentException>(() => TaxaEfetivaCalculator.Calcular(taxaNominal, capitalizacoes));
    }
}
