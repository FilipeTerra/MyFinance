using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class FinanciamentoPriceCalculatorTests
{
    [Fact]
    public void Calcular_WithKnownValues_MatchesClosedFormFormula()
    {
        // Mesmo caso base do tools.py (calcular_juros_financiamento): PV=50000,
        // i=1,5% a.m., 48x. Valor conferido de forma independente com a fórmula
        // da Tabela Price (PMT = PV * i / (1 - (1+i)^-n)) -> parcela ~1468.75,
        // juros totais ~20500.00.
        var resultado = FinanciamentoPriceCalculator.Calcular(50000m, 1.5m, 48);

        Assert.Equal(1468.75m, resultado.ValorParcela, 1);
        Assert.Equal(20500.00m, resultado.TotalJuros, 0);
    }

    [Fact]
    public void Calcular_ParcelaIsConstantExceptForTheLiquidatingOne()
    {
        var resultado = FinanciamentoPriceCalculator.Calcular(10000m, 2m, 12);

        // A parcela é fixa em todo o contrato menos na última: é ela que liquida o
        // resíduo de arredondamento (aqui, R$ 0,05 a menos), para o saldo devedor
        // fechar em zero exato em vez de sobrar centavos.
        Assert.All(resultado.Parcelas.SkipLast(1), p => Assert.Equal(resultado.ValorParcela, p.ValorParcela));

        var ultima = resultado.Parcelas[^1];
        Assert.True(ultima.ValorParcela <= resultado.ValorParcela);
        Assert.Equal(ultima.Juros + ultima.Amortizacao, ultima.ValorParcela);
    }

    [Fact]
    public void Calcular_SaldoDevedorReachesZeroAtLastInstallment()
    {
        var resultado = FinanciamentoPriceCalculator.Calcular(10000m, 2m, 12);

        Assert.Equal(0m, resultado.Parcelas[^1].SaldoDevedor);
    }

    [Fact]
    public void Calcular_TotalPagoEqualsPrincipalPlusInterest()
    {
        var resultado = FinanciamentoPriceCalculator.Calcular(50000m, 1.5m, 48);

        // O total pago passou a ser acumulado linha a linha em vez de estimado por
        // parcela × prazo, então tem que fechar exatamente com principal + juros.
        Assert.Equal(50000m + resultado.TotalJuros, resultado.TotalPago);
        Assert.Equal(50000m, resultado.Parcelas.Sum(p => p.Amortizacao));
    }

    [Fact]
    public void Calcular_WithZeroRate_InstallmentIsPrincipalDividedByCount()
    {
        var resultado = FinanciamentoPriceCalculator.Calcular(12000m, 0m, 12);

        Assert.Equal(1000m, resultado.ValorParcela);
        Assert.Equal(0m, resultado.TotalJuros);
    }

    [Theory]
    [InlineData(0, 1, 12)]
    [InlineData(-1, 1, 12)]
    [InlineData(10000, -1, 12)]
    [InlineData(10000, 1, 0)]
    [InlineData(10000, 1, -1)]
    public void Calcular_WithInvalidArguments_ThrowsArgumentException(decimal valorFinanciado, decimal taxa, int numParcelas)
    {
        Assert.Throws<ArgumentException>(() => FinanciamentoPriceCalculator.Calcular(valorFinanciado, taxa, numParcelas));
    }
}
