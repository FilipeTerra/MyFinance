using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class ImpostoRendaCalculatorTests
{
    [Theory]
    [InlineData(6, 22.5)]
    [InlineData(12, 20)]
    [InlineData(24, 17.5)]
    [InlineData(25, 15)]
    [InlineData(120, 15)]
    public void ObterAliquotaRegressiva_ReturnsBracketMatchingPrazo(int prazoMeses, decimal aliquotaEsperada)
    {
        Assert.Equal(aliquotaEsperada, ImpostoRendaCalculator.ObterAliquotaRegressiva(prazoMeses));
    }

    [Fact]
    public void Calcular_WhenIsento_ReturnsZeroTaxAndFullValue()
    {
        var resultado = ImpostoRendaCalculator.Calcular(totalJuros: 1000m, valorFinal: 11000m, prazoMeses: 6, isento: true);

        Assert.Equal(0m, resultado.AliquotaPercentual);
        Assert.Equal(0m, resultado.ValorImposto);
        Assert.Equal(11000m, resultado.ValorLiquido);
    }

    [Fact]
    public void Calcular_WhenTributavel_AppliesRateOnlyOnEarnings()
    {
        var resultado = ImpostoRendaCalculator.Calcular(totalJuros: 1000m, valorFinal: 11000m, prazoMeses: 25, isento: false);

        Assert.Equal(15m, resultado.AliquotaPercentual);
        Assert.Equal(150m, resultado.ValorImposto);
        Assert.Equal(10850m, resultado.ValorLiquido);
    }

    [Fact]
    public void Calcular_WithNoEarnings_ReturnsZeroTax()
    {
        var resultado = ImpostoRendaCalculator.Calcular(totalJuros: 0m, valorFinal: 10000m, prazoMeses: 12, isento: false);

        Assert.Equal(0m, resultado.AliquotaPercentual);
        Assert.Equal(0m, resultado.ValorImposto);
        Assert.Equal(10000m, resultado.ValorLiquido);
    }
}
