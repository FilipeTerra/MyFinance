using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class FinanciamentoSacCalculatorTests
{
    [Fact]
    public void Calcular_WithSlideExample_MatchesHandComputedInstallments()
    {
        // Exemplo dos slides do Cap 5 (Sistema de Amortização Constante):
        // PV=10000, i=4% a.m., 4x -> amortização de 2500/mês, parcelas
        // 2900/2800/2700/2600, juros totais de 1000.
        var resultado = FinanciamentoSacCalculator.Calcular(10000m, 4m, 4);

        Assert.Equal(2900m, resultado.Parcelas[0].ValorParcela);
        Assert.Equal(2800m, resultado.Parcelas[1].ValorParcela);
        Assert.Equal(2700m, resultado.Parcelas[2].ValorParcela);
        Assert.Equal(2600m, resultado.Parcelas[3].ValorParcela);
        Assert.Equal(1000m, resultado.TotalJuros);
        Assert.Equal(11000m, resultado.TotalPago);
    }

    [Fact]
    public void Calcular_InstallmentsAreStrictlyDecreasingWhenRateIsPositive()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(20000m, 1.5m, 24);

        for (var idx = 1; idx < resultado.Parcelas.Count; idx++)
            Assert.True(resultado.Parcelas[idx].ValorParcela < resultado.Parcelas[idx - 1].ValorParcela);
    }

    [Fact]
    public void Calcular_AmortizacaoIsConstantAcrossAllMonths()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(12000m, 2m, 12);

        Assert.All(resultado.Parcelas, p => Assert.Equal(1000m, p.Amortizacao));
    }

    [Fact]
    public void Calcular_SaldoDevedorReachesZeroAtLastInstallment()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(12000m, 2m, 12);

        Assert.Equal(0m, resultado.Parcelas[^1].SaldoDevedor);
    }

    [Theory]
    [InlineData(0, 1, 12)]
    [InlineData(-1, 1, 12)]
    [InlineData(10000, -1, 12)]
    [InlineData(10000, 1, 0)]
    public void Calcular_WithInvalidArguments_ThrowsArgumentException(decimal valorFinanciado, decimal taxa, int numParcelas)
    {
        Assert.Throws<ArgumentException>(() => FinanciamentoSacCalculator.Calcular(valorFinanciado, taxa, numParcelas));
    }
}
