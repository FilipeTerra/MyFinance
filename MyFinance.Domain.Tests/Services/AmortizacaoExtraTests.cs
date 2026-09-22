using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

/// <summary>
/// Comportamento da amortização extra, exercitado pelas duas portas públicas
/// (Price e SAC) — o motor que a aplica é interno e não é testado diretamente.
/// </summary>
public class AmortizacaoExtraTests
{
    private static AmortizacaoExtra Mensal(decimal valor, ModoAmortizacaoExtra modo) =>
        new(valor, null, modo);

    private static AmortizacaoExtra Avulsa(int mes, decimal valor, ModoAmortizacaoExtra modo) =>
        new(0m, new[] { new AmortizacaoExtraAvulsa(mes, valor) }, modo);

    // ---------- Anti-regressão ----------

    [Theory]
    [InlineData(ModoAmortizacaoExtra.ReduzirPrazo)]
    [InlineData(ModoAmortizacaoExtra.ReduzirParcela)]
    public void Calcular_WithZeroExtra_MatchesTheContractWithoutAnyExtra(ModoAmortizacaoExtra modo)
    {
        var semExtra = FinanciamentoPriceCalculator.Calcular(200000m, 0.8m, 240);
        var comExtraZerada = FinanciamentoPriceCalculator.Calcular(200000m, 0.8m, 240, Mensal(0m, modo));

        Assert.Equal(semExtra.TotalPago, comExtraZerada.TotalPago);
        Assert.Equal(semExtra.TotalJuros, comExtraZerada.TotalJuros);
        Assert.Equal(semExtra.PrazoFinalMeses, comExtraZerada.PrazoFinalMeses);
    }

    // ---------- Reduzir prazo ----------

    [Fact]
    public void Calcular_ReduzirPrazo_QuitaAntesEEconomizaJuros()
    {
        var semExtra = FinanciamentoSacCalculator.Calcular(200000m, 0.8m, 240);
        var comExtra = FinanciamentoSacCalculator.Calcular(
            200000m, 0.8m, 240, Mensal(2000m, ModoAmortizacaoExtra.ReduzirPrazo));

        Assert.True(comExtra.PrazoFinalMeses < semExtra.PrazoFinalMeses);
        Assert.True(comExtra.TotalJuros < semExtra.TotalJuros);
        Assert.Equal(comExtra.PrazoFinalMeses, comExtra.Parcelas.Count);
    }

    [Fact]
    public void Calcular_ReduzirPrazo_MantemAParcelaContratualDoPrice()
    {
        var comExtra = FinanciamentoPriceCalculator.Calcular(
            200000m, 0.8m, 240, Mensal(2000m, ModoAmortizacaoExtra.ReduzirPrazo));

        // O que muda é quando o contrato acaba, não quanto se paga por mês.
        Assert.All(comExtra.Parcelas.SkipLast(1), p => Assert.Equal(comExtra.ValorParcela, p.ValorParcela));
    }

    // ---------- Reduzir parcela ----------

    [Fact]
    public void Calcular_ReduzirParcela_ComAporteUnico_MantemOPrazoEBaixaAParcela()
    {
        var comExtra = FinanciamentoPriceCalculator.Calcular(
            200000m, 0.8m, 240, Avulsa(24, 30000m, ModoAmortizacaoExtra.ReduzirParcela));

        Assert.Equal(240, comExtra.PrazoFinalMeses);
        // A parcela do mês seguinte ao aporte já vem recalculada sobre o saldo menor.
        Assert.True(comExtra.Parcelas[24].ValorParcela < comExtra.Parcelas[23].ValorParcela);
    }

    [Fact]
    public void Calcular_ReduzirParcela_ComExtraMensal_AindaAntecipaAQuitacao()
    {
        // Recalcular a parcela devolve folga no mês, mas quem continua pagando o
        // extra por cima da parcela nova segue adiantando o contrato: o prazo não
        // tem como ser preservado quando o pagamento extra é recorrente.
        var comExtra = FinanciamentoPriceCalculator.Calcular(
            200000m, 0.8m, 240, Mensal(2000m, ModoAmortizacaoExtra.ReduzirParcela));

        Assert.True(comExtra.PrazoFinalMeses < 240);
        Assert.True(comExtra.Parcelas[^1].ValorParcela < comExtra.Parcelas[0].ValorParcela);
    }

    [Fact]
    public void Calcular_ReduzirPrazoEconomizaMaisQueReduzirParcela()
    {
        // Com o mesmo dinheiro extra, encurtar o prazo tira mais juros do
        // contrato do que aliviar a parcela — é a razão de o seletor existir.
        var prazo = FinanciamentoSacCalculator.Calcular(
            200000m, 0.8m, 240, Mensal(1000m, ModoAmortizacaoExtra.ReduzirPrazo));
        var parcela = FinanciamentoSacCalculator.Calcular(
            200000m, 0.8m, 240, Mensal(1000m, ModoAmortizacaoExtra.ReduzirParcela));

        Assert.True(prazo.TotalJuros < parcela.TotalJuros);
    }

    // ---------- Avulsas ----------

    [Fact]
    public void Calcular_ComAvulsa_AbateSomenteNoMesIndicado()
    {
        var semExtra = FinanciamentoSacCalculator.Calcular(100000m, 1m, 120);
        var comExtra = FinanciamentoSacCalculator.Calcular(
            100000m, 1m, 120, Avulsa(24, 10000m, ModoAmortizacaoExtra.ReduzirPrazo));

        Assert.Equal(0m, comExtra.Parcelas[22].AmortizacaoExtra);
        Assert.Equal(10000m, comExtra.Parcelas[23].AmortizacaoExtra);
        Assert.Equal(0m, comExtra.Parcelas[24].AmortizacaoExtra);
        Assert.Equal(semExtra.Parcelas[22].SaldoDevedor, comExtra.Parcelas[22].SaldoDevedor);
    }

    [Fact]
    public void Calcular_ComDuasAvulsasNoMesmoMes_SomaOsValores()
    {
        var extra = new AmortizacaoExtra(
            0m,
            new[] { new AmortizacaoExtraAvulsa(12, 5000m), new AmortizacaoExtraAvulsa(12, 3000m) },
            ModoAmortizacaoExtra.ReduzirPrazo);

        var resultado = FinanciamentoSacCalculator.Calcular(100000m, 1m, 120, extra);

        Assert.Equal(8000m, resultado.Parcelas[11].AmortizacaoExtra);
    }

    [Fact]
    public void Calcular_ComAmortizacaoExtraMaiorQueOSaldo_NaoGeraSaldoNegativo()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(
            10000m, 1m, 12, Avulsa(2, 999999m, ModoAmortizacaoExtra.ReduzirPrazo));

        Assert.Equal(2, resultado.PrazoFinalMeses);
        Assert.Equal(0m, resultado.Parcelas[^1].SaldoDevedor);
        Assert.All(resultado.Parcelas, p => Assert.True(p.SaldoDevedor >= 0m));
        // O extra é limitado ao que realmente faltava — o resto nunca é cobrado.
        Assert.True(resultado.Parcelas[^1].AmortizacaoExtra < 999999m);
    }

    [Fact]
    public void Calcular_TotalPagoSegueFechandoComPrincipalMaisJuros()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(
            200000m, 0.8m, 240, Mensal(2000m, ModoAmortizacaoExtra.ReduzirPrazo));

        Assert.Equal(200000m + resultado.TotalJuros, resultado.TotalPago);
        Assert.Equal(
            200000m,
            resultado.Parcelas.Sum(p => p.Amortizacao) + resultado.Parcelas.Sum(p => p.AmortizacaoExtra));
    }

    [Fact]
    public void Calcular_TotalAmortizacaoExtraSomaAsLinhas()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(
            200000m, 0.8m, 240, Mensal(2000m, ModoAmortizacaoExtra.ReduzirPrazo));

        Assert.Equal(resultado.Parcelas.Sum(p => p.AmortizacaoExtra), resultado.TotalAmortizacaoExtra);
    }

    // ---------- Entradas inválidas ----------

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Calcular_ComAvulsaForaDoPrazo_ThrowsArgumentException(int mes)
    {
        Assert.Throws<ArgumentException>(() => FinanciamentoSacCalculator.Calcular(
            10000m, 1m, 12, Avulsa(mes, 500m, ModoAmortizacaoExtra.ReduzirPrazo)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Calcular_ComAvulsaSemValorPositivo_ThrowsArgumentException(decimal valor)
    {
        Assert.Throws<ArgumentException>(() => FinanciamentoSacCalculator.Calcular(
            10000m, 1m, 12, Avulsa(6, valor, ModoAmortizacaoExtra.ReduzirPrazo)));
    }

    [Fact]
    public void Calcular_ComExtraMensalNegativa_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => FinanciamentoSacCalculator.Calcular(
            10000m, 1m, 12, Mensal(-1m, ModoAmortizacaoExtra.ReduzirPrazo)));
    }
}
