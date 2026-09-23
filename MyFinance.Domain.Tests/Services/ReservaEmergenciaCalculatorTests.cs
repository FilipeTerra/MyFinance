using MyFinance.Domain.Services;
using static MyFinance.Domain.Services.ReservaEmergenciaCalculator;

namespace MyFinance.Domain.Tests.Services;

public class ReservaEmergenciaCalculatorTests
{
    [Fact]
    public void Calcular_WithoutIncome_ReturnsNull()
    {
        Assert.Null(ReservaEmergenciaCalculator.Calcular(0m, Array.Empty<MetaGuardada>(), 1000m));
        Assert.Null(ReservaEmergenciaCalculator.Calcular(-100m, Array.Empty<MetaGuardada>(), 1000m));
    }

    [Fact]
    public void Calcular_WhenSavingsCoverSixMonths_IsAdequate()
    {
        var metas = new[] { new MetaGuardada("Reserva de emergência", 30000m) };

        var resultado = ReservaEmergenciaCalculator.Calcular(5000m, metas, 0m)!;

        Assert.True(resultado.Adequada);
        Assert.Equal(30000m, resultado.ValorIdeal);
        Assert.Equal(0m, resultado.ValorFaltante);
        Assert.Equal(100m, resultado.PercentualAtingido);
        Assert.Equal(6m, resultado.MesesCobertos);
    }

    [Fact]
    public void Calcular_SumsReserveGoalsAndFixedIncomeInvestments()
    {
        var metas = new[]
        {
            new MetaGuardada("Minha RESERVA", 5000m),
            new MetaGuardada("Viagem ao Japão", 20000m), // não é reserva: fica de fora
        };

        var resultado = ReservaEmergenciaCalculator.Calcular(2000m, metas, 1000m)!;

        Assert.Equal(6000m, resultado.ValorAtual);
        Assert.Equal(12000m, resultado.ValorIdeal);
        Assert.Equal(6000m, resultado.ValorFaltante);
        Assert.Equal(50m, resultado.PercentualAtingido);
        Assert.Equal(3m, resultado.MesesCobertos);
        Assert.False(resultado.Adequada);
    }

    [Fact]
    public void Calcular_MatchesGoalNameIgnoringCaseAndAccents()
    {
        var metas = new[] { new MetaGuardada("FUNDO RESERVA", 1200m) };

        var resultado = ReservaEmergenciaCalculator.Calcular(1000m, metas, 0m)!;

        Assert.Equal(1200m, resultado.ValorAtual);
    }

    [Fact]
    public void Calcular_WithNoSavings_ReportsZeroProgress()
    {
        var resultado = ReservaEmergenciaCalculator.Calcular(3000m, Array.Empty<MetaGuardada>(), 0m)!;

        Assert.False(resultado.Adequada);
        Assert.Equal(0m, resultado.ValorAtual);
        Assert.Equal(18000m, resultado.ValorFaltante);
        Assert.Equal(0m, resultado.PercentualAtingido);
    }
}
