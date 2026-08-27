using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class RetiradaCalculatorTests
{
    // ---------- CalcularSaqueMaximoSustentavel ----------

    [Fact]
    public void CalcularSaqueMaximoSustentavel_WithZeroRate_DividesEvenly()
    {
        var saque = RetiradaCalculator.CalcularSaqueMaximoSustentavel(saldoInicial: 120000m, taxaJurosAnualPercentual: 0m, prazoMeses: 120);

        Assert.Equal(1000m, saque);
    }

    [Fact]
    public void CalcularSaqueMaximoSustentavel_DepletesBalanceByEndOfPrazo()
    {
        const decimal saldoInicial = 500000m;
        var saque = RetiradaCalculator.CalcularSaqueMaximoSustentavel(saldoInicial, taxaJurosAnualPercentual: 6m, prazoMeses: 300);

        var resultado = RetiradaCalculator.Simular(saldoInicial, 0m, saque, 6m, 300, CategoriaTributariaAtivo.RendaFixaIsenta);

        // A fórmula fechada usa o saque arredondado a centavos, então sobra um
        // resíduo desprezível (frações de real) em vez de esgotar exatamente —
        // o arredondamento sempre favorece o investidor, nunca deixa faltar.
        var saldoFinal = resultado.Evolucao[^1].SaldoFinal;
        Assert.False(resultado.Esgotou);
        Assert.True(saldoFinal >= 0);
        Assert.True(saldoFinal < saldoInicial * 0.0001m);
    }

    [Fact]
    public void CalcularSaqueMaximoSustentavel_WithInvalidInputs_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => RetiradaCalculator.CalcularSaqueMaximoSustentavel(0m, 6m, 120));
        Assert.Throws<ArgumentException>(() => RetiradaCalculator.CalcularSaqueMaximoSustentavel(1000m, -1m, 120));
        Assert.Throws<ArgumentException>(() => RetiradaCalculator.CalcularSaqueMaximoSustentavel(1000m, 6m, 0));
    }

    // ---------- CalcularMesesAteEsgotar ----------

    [Fact]
    public void CalcularMesesAteEsgotar_WithZeroRate_DividesEvenly()
    {
        var meses = RetiradaCalculator.CalcularMesesAteEsgotar(saldoInicial: 12000m, saqueMensal: 1000m, taxaJurosAnualPercentual: 0m);

        Assert.Equal(12, meses);
    }

    [Fact]
    public void CalcularMesesAteEsgotar_WhenSaqueBelowMonthlyYield_ReturnsNull()
    {
        // Rendimento mensal de 1.000.000 a 12% a.a. é bem maior que um saque de 1.000/mês.
        var meses = RetiradaCalculator.CalcularMesesAteEsgotar(saldoInicial: 1_000_000m, saqueMensal: 1000m, taxaJurosAnualPercentual: 12m);

        Assert.Null(meses);
    }

    [Fact]
    public void CalcularMesesAteEsgotar_IsConsistentWithSaqueMaximoSustentavel()
    {
        var saque = RetiradaCalculator.CalcularSaqueMaximoSustentavel(saldoInicial: 200000m, taxaJurosAnualPercentual: 8m, prazoMeses: 240);
        var meses = RetiradaCalculator.CalcularMesesAteEsgotar(saldoInicial: 200000m, saqueMensal: saque, taxaJurosAnualPercentual: 8m);

        Assert.NotNull(meses);
        Assert.InRange(meses!.Value, 239, 241);
    }

    // ---------- Simular (IR por saque) ----------

    [Fact]
    public void Simular_RendaFixaIsenta_NeverWithholdsTax()
    {
        var resultado = RetiradaCalculator.Simular(100000m, 40000m, 2000m, 6m, 12, CategoriaTributariaAtivo.RendaFixaIsenta);

        Assert.All(resultado.Evolucao, mes => Assert.Equal(0m, mes.ValorImposto));
        Assert.All(resultado.Evolucao, mes => Assert.Equal(mes.SaqueBruto, mes.SaqueLiquido));
    }

    [Fact]
    public void Simular_RendaFixaTributavel_WithholdsFloorRateOnGainPortionOnly()
    {
        var resultado = RetiradaCalculator.Simular(100000m, 40000m, 2000m, 6m, 1, CategoriaTributariaAtivo.RendaFixaTributavel);

        var mes1 = resultado.Evolucao[0];
        Assert.Equal(15m, mes1.AliquotaImpostoPercentual);
        Assert.True(mes1.ValorImposto > 0);
        // Base de custo é 40% do saldo, então o ganho é ~60% do saque — imposto bem menor que 15% do saque bruto.
        Assert.True(mes1.ValorImposto < mes1.SaqueBruto * 0.15m);
    }

    [Fact]
    public void Simular_Pgbl_TaxesFullWithdrawalNotJustGain()
    {
        var resultado = RetiradaCalculator.Simular(100000m, 0m, 2000m, 0m, 1, CategoriaTributariaAtivo.PrevidenciaPgbl);

        var mes1 = resultado.Evolucao[0];
        // PGBL tributa o saque inteiro (base de custo já foi deduzida do IR na entrada).
        Assert.Equal(10m, mes1.AliquotaImpostoPercentual);
        Assert.Equal(Math.Round(2000m * 0.10m, 2), mes1.ValorImposto);
    }

    [Fact]
    public void Simular_GanhoCapitalAcao_IsentoWhenWithdrawalBelowThreshold()
    {
        var resultado = RetiradaCalculator.Simular(100000m, 50000m, 5000m, 6m, 1, CategoriaTributariaAtivo.GanhoCapitalAcao);

        // Saque de 5.000 < limite de isenção de 20.000 → isento nesse mês.
        Assert.Equal(0m, resultado.Evolucao[0].ValorImposto);
    }

    [Fact]
    public void Simular_StopsAtDepletionMonth()
    {
        var resultado = RetiradaCalculator.Simular(1000m, 0m, 10000m, 0m, 12, CategoriaTributariaAtivo.RendaFixaIsenta);

        Assert.True(resultado.Esgotou);
        Assert.Equal(1, resultado.MesEsgotamento);
        Assert.Single(resultado.Evolucao);
        Assert.Equal(1000m, resultado.Evolucao[0].SaqueBruto); // limitado ao saldo disponível
        Assert.Equal(0m, resultado.Evolucao[0].SaldoFinal);
    }

    [Fact]
    public void Simular_WithInvalidInputs_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => RetiradaCalculator.Simular(0m, 0m, 100m, 6m, 12, CategoriaTributariaAtivo.RendaFixaIsenta));
        Assert.Throws<ArgumentException>(() => RetiradaCalculator.Simular(1000m, 2000m, 100m, 6m, 12, CategoriaTributariaAtivo.RendaFixaIsenta));
        Assert.Throws<ArgumentException>(() => RetiradaCalculator.Simular(1000m, 0m, 0m, 6m, 12, CategoriaTributariaAtivo.RendaFixaIsenta));
        Assert.Throws<ArgumentException>(() => RetiradaCalculator.Simular(1000m, 0m, 100m, 6m, 0, CategoriaTributariaAtivo.RendaFixaIsenta));
    }
}
