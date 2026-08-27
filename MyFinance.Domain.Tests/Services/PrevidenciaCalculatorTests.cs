using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class PrevidenciaCalculatorTests
{
    [Theory]
    [InlineData(24, 35)]
    [InlineData(48, 30)]
    [InlineData(72, 25)]
    [InlineData(96, 20)]
    [InlineData(120, 15)]
    [InlineData(121, 10)]
    [InlineData(240, 10)]
    public void ObterAliquotaRegressiva_ReturnsBracketMatchingPrazo(int prazoMeses, decimal aliquotaEsperada)
    {
        Assert.Equal(aliquotaEsperada, PrevidenciaCalculator.ObterAliquotaRegressiva(prazoMeses));
    }

    [Fact]
    public void Calcular_Pgbl_TaxesFullWithdrawalAmount()
    {
        var resultado = PrevidenciaCalculator.Calcular(
            totalJuros: 1000m, valorFinal: 11000m, prazoMeses: 24, categoria: CategoriaTributariaAtivo.PrevidenciaPgbl);

        Assert.Equal(35m, resultado.AliquotaPercentual);
        Assert.Equal(3850m, resultado.ValorImposto); // 35% sobre os 11000 totais, não só sobre o ganho
        Assert.Equal(7150m, resultado.ValorLiquido);
    }

    [Fact]
    public void Calcular_Vgbl_TaxesOnlyEarnings()
    {
        var resultado = PrevidenciaCalculator.Calcular(
            totalJuros: 1000m, valorFinal: 11000m, prazoMeses: 24, categoria: CategoriaTributariaAtivo.PrevidenciaVgbl);

        Assert.Equal(35m, resultado.AliquotaPercentual);
        Assert.Equal(350m, resultado.ValorImposto); // 35% só sobre os 1000 de rendimento
        Assert.Equal(10650m, resultado.ValorLiquido);
    }

    [Fact]
    public void Calcular_WithNoEarnings_ReturnsZeroTax()
    {
        var resultado = PrevidenciaCalculator.Calcular(
            totalJuros: 0m, valorFinal: 10000m, prazoMeses: 24, categoria: CategoriaTributariaAtivo.PrevidenciaVgbl);

        Assert.Equal(0m, resultado.AliquotaPercentual);
        Assert.Equal(0m, resultado.ValorImposto);
        Assert.Equal(10000m, resultado.ValorLiquido);
    }

    [Fact]
    public void Calcular_WithNonPrevidenciaCategoria_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PrevidenciaCalculator.Calcular(1000m, 11000m, 24, CategoriaTributariaAtivo.RendaFixaTributavel));
    }
}
