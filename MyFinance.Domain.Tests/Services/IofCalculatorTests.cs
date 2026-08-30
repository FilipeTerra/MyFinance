using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class IofCalculatorTests
{
    [Theory]
    [InlineData(1, 96)]
    [InlineData(15, 50)]
    [InlineData(29, 3)]
    [InlineData(30, 0)]
    [InlineData(90, 0)]
    public void ObterAliquotaRegressiva_ReturnsRateMatchingDay(int diasCorridos, decimal aliquotaEsperada)
    {
        Assert.Equal(aliquotaEsperada, IofCalculator.ObterAliquotaRegressiva(diasCorridos));
    }

    [Fact]
    public void Calcular_WithEarlyRedemption_AppliesRateOverEarnings()
    {
        var resultado = IofCalculator.Calcular(totalJuros: 1000m, diasCorridos: 1);

        Assert.Equal(96m, resultado.AliquotaPercentual);
        Assert.Equal(960m, resultado.ValorIof);
        Assert.Equal(40m, resultado.RendimentoLiquido);
    }

    [Fact]
    public void Calcular_AtOrAfter30Days_ReturnsZeroTax()
    {
        var resultado = IofCalculator.Calcular(totalJuros: 1000m, diasCorridos: 30);

        Assert.Equal(0m, resultado.AliquotaPercentual);
        Assert.Equal(0m, resultado.ValorIof);
        Assert.Equal(1000m, resultado.RendimentoLiquido);
    }

    [Fact]
    public void Calcular_WithNoEarnings_ReturnsZeroTax()
    {
        var resultado = IofCalculator.Calcular(totalJuros: 0m, diasCorridos: 5);

        Assert.Equal(0m, resultado.AliquotaPercentual);
        Assert.Equal(0m, resultado.ValorIof);
        Assert.Equal(0m, resultado.RendimentoLiquido);
    }
}
