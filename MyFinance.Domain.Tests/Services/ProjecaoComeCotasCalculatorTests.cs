using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class ProjecaoComeCotasCalculatorTests
{
    [Fact]
    public void Calcular_BeforeFirstSemester_NoComeCotasWithheld()
    {
        var resultado = ProjecaoComeCotasCalculator.Calcular(
            aporteInicial: 10000m, aporteMensal: 0m, taxaJurosAnualPercentual: 12m, meses: 5, aliquotaComeCotas: 15m);

        Assert.Equal(0m, resultado.TotalComeCotasRetido);
        Assert.All(resultado.Evolucao, mes => Assert.Equal(0m, mes.ComeCotasRetidoNoMes));
    }

    [Fact]
    public void Calcular_AtSixthMonth_WithholdsOnAccumulatedGain()
    {
        var resultado = ProjecaoComeCotasCalculator.Calcular(
            aporteInicial: 10000m, aporteMensal: 0m, taxaJurosAnualPercentual: 12m, meses: 6, aliquotaComeCotas: 15m);

        Assert.True(resultado.TotalComeCotasRetido > 0);
        Assert.Equal(resultado.TotalComeCotasRetido, resultado.Evolucao[5].ComeCotasRetidoNoMes);
        Assert.All(resultado.Evolucao.Take(5), mes => Assert.Equal(0m, mes.ComeCotasRetidoNoMes));
    }

    [Fact]
    public void Calcular_ValorFinalPlusRetidoMinusAportado_EqualsTotalGanhoBruto()
    {
        var resultado = ProjecaoComeCotasCalculator.Calcular(
            aporteInicial: 10000m, aporteMensal: 500m, taxaJurosAnualPercentual: 12m, meses: 24, aliquotaComeCotas: 15m);

        var ganhoBrutoRecalculado = resultado.ValorFinal + resultado.TotalComeCotasRetido - resultado.TotalAportado;
        Assert.Equal(resultado.TotalGanhoBruto, ganhoBrutoRecalculado, 2);
    }

    [Fact]
    public void Calcular_HigherAliquota_RetainsMoreThanLowerAliquota()
    {
        var longoPrazo = ProjecaoComeCotasCalculator.Calcular(10000m, 0m, 12m, 24, aliquotaComeCotas: 15m);
        var curtoPrazo = ProjecaoComeCotasCalculator.Calcular(10000m, 0m, 12m, 24, aliquotaComeCotas: 20m);

        Assert.True(curtoPrazo.TotalComeCotasRetido > longoPrazo.TotalComeCotasRetido);
    }

    [Fact]
    public void Calcular_WithZeroRate_NeverWithholds()
    {
        var resultado = ProjecaoComeCotasCalculator.Calcular(
            aporteInicial: 0m, aporteMensal: 1000m, taxaJurosAnualPercentual: 0m, meses: 24, aliquotaComeCotas: 15m);

        Assert.Equal(0m, resultado.TotalComeCotasRetido);
        Assert.Equal(0m, resultado.TotalGanhoBruto);
    }

    [Fact]
    public void Calcular_GeneratesOneEvolucaoEntryPerMonth()
    {
        var resultado = ProjecaoComeCotasCalculator.Calcular(1000m, 100m, 10m, 18, 15m);

        Assert.Equal(18, resultado.Evolucao.Count);
        Assert.Equal(1, resultado.Evolucao[0].Mes);
        Assert.Equal(18, resultado.Evolucao[^1].Mes);
    }

    [Theory]
    [InlineData(-1, 0, 12, 12)]
    [InlineData(1000, -1, 12, 12)]
    [InlineData(1000, 0, -1, 12)]
    [InlineData(1000, 0, 12, 0)]
    public void Calcular_WithInvalidInputs_ThrowsArgumentException(decimal aporteInicial, decimal aporteMensal, decimal taxa, int meses)
    {
        Assert.Throws<ArgumentException>(() =>
            ProjecaoComeCotasCalculator.Calcular(aporteInicial, aporteMensal, taxa, meses, 15m));
    }

    [Fact]
    public void Calcular_WithAporteExtra_AddsToTotalAportadoInTheGivenMonth()
    {
        var semExtra = ProjecaoComeCotasCalculator.Calcular(0m, 100m, 12m, 3, 15m);
        var comExtra = ProjecaoComeCotasCalculator.Calcular(0m, 100m, 12m, 3, 15m, new[] { new AporteExtra(2, 1000m) });

        Assert.Equal(semExtra.TotalAportado + 1000m, comExtra.TotalAportado);
    }

    [Fact]
    public void Calcular_WithReajusteAnual_IncreasesAporteAtMonthThirteen()
    {
        var resultado = ProjecaoComeCotasCalculator.Calcular(0m, 100m, 0m, 13, 15m, reajusteAnualPercentual: 20m);

        // Sem come-cotas nesse período curto, então o total aportado reflete só os aportes.
        Assert.Equal(100m * 12 + 120m, resultado.TotalAportado);
    }
}
