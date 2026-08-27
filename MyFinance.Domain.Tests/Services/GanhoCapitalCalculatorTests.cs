using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class GanhoCapitalCalculatorTests
{
    [Fact]
    public void Calcular_Acao_AboveThreshold_Applies15Percent()
    {
        var resultado = GanhoCapitalCalculator.Calcular(
            totalJuros: 6000m, valorFinal: 56000m, categoria: CategoriaTributariaAtivo.GanhoCapitalAcao);

        Assert.False(resultado.Isento);
        Assert.Equal(15m, resultado.AliquotaPercentual);
        Assert.Equal(900m, resultado.ValorImposto);
        Assert.Equal(55100m, resultado.ValorLiquido);
    }

    [Fact]
    public void Calcular_Acao_BelowThreshold_IsIsento()
    {
        var resultado = GanhoCapitalCalculator.Calcular(
            totalJuros: 120m, valorFinal: 1120m, categoria: CategoriaTributariaAtivo.GanhoCapitalAcao);

        Assert.True(resultado.Isento);
        Assert.Equal(0m, resultado.AliquotaPercentual);
        Assert.Equal(0m, resultado.ValorImposto);
        Assert.Equal(1120m, resultado.ValorLiquido);
    }

    [Fact]
    public void Calcular_Cripto_AboveThreshold_Applies15Percent()
    {
        var resultado = GanhoCapitalCalculator.Calcular(
            totalJuros: 7200m, valorFinal: 67200m, categoria: CategoriaTributariaAtivo.GanhoCapitalCripto);

        Assert.False(resultado.Isento);
        Assert.Equal(15m, resultado.AliquotaPercentual);
        Assert.Equal(1080m, resultado.ValorImposto);
    }

    [Fact]
    public void Calcular_Cripto_BelowThreshold_IsIsento()
    {
        var resultado = GanhoCapitalCalculator.Calcular(
            totalJuros: 120m, valorFinal: 1120m, categoria: CategoriaTributariaAtivo.GanhoCapitalCripto);

        Assert.True(resultado.Isento);
        Assert.Equal(0m, resultado.ValorImposto);
    }

    [Fact]
    public void Calcular_Fii_Applies20Percent_NoThresholdIsencao()
    {
        var resultado = GanhoCapitalCalculator.Calcular(
            totalJuros: 120m, valorFinal: 1120m, categoria: CategoriaTributariaAtivo.GanhoCapitalFii);

        Assert.False(resultado.Isento);
        Assert.Equal(20m, resultado.AliquotaPercentual);
        Assert.Equal(24m, resultado.ValorImposto);
    }

    [Fact]
    public void Calcular_FundoAcoes_Applies15Percent_NoThresholdIsencao()
    {
        var resultado = GanhoCapitalCalculator.Calcular(
            totalJuros: 120m, valorFinal: 1120m, categoria: CategoriaTributariaAtivo.GanhoCapitalFundoAcoes);

        Assert.False(resultado.Isento);
        Assert.Equal(15m, resultado.AliquotaPercentual);
        Assert.Equal(18m, resultado.ValorImposto);
    }

    [Fact]
    public void Calcular_WithNoEarnings_ReturnsZeroTax()
    {
        var resultado = GanhoCapitalCalculator.Calcular(
            totalJuros: 0m, valorFinal: 10000m, categoria: CategoriaTributariaAtivo.GanhoCapitalAcao);

        Assert.True(resultado.Isento);
        Assert.Equal(0m, resultado.ValorImposto);
        Assert.Equal(10000m, resultado.ValorLiquido);
    }

    [Fact]
    public void Calcular_WithRendaFixaCategoria_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            GanhoCapitalCalculator.Calcular(1000m, 11000m, CategoriaTributariaAtivo.RendaFixaTributavel));
    }
}
