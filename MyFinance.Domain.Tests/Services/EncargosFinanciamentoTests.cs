using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

/// <summary>
/// Comportamento dos seguros obrigatórios e da taxa de administração,
/// exercitado pelas portas públicas — o motor que os aplica é interno.
/// </summary>
public class EncargosFinanciamentoTests
{
    // MIP de 0,025% a.m. sobre o saldo, DFI de 0,01% a.m. sobre o imóvel, R$ 25 de tarifa.
    private static EncargosFinanciamento Encargos(
        decimal mip = 0.025m, decimal dfi = 0.01m, decimal imovel = 500000m, decimal taxa = 25m) =>
        new(mip, dfi, imovel, taxa);

    [Fact]
    public void Calcular_EncargosNaoAlteramOSaldoDevedor()
    {
        var sem = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360);
        var com = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360, null, Encargos());

        // O seguro é despesa, não dívida: a amortização do principal é a mesma.
        Assert.Equal(sem.TotalJuros, com.TotalJuros);
        Assert.Equal(sem.TotalPago, com.TotalPago);
        Assert.Equal(sem.PrazoFinalMeses, com.PrazoFinalMeses);
        Assert.All(
            com.Parcelas.Zip(sem.Parcelas),
            par => Assert.Equal(par.Second.SaldoDevedor, par.First.SaldoDevedor));
    }

    [Fact]
    public void Calcular_MipCaiJuntoComOSaldoDevedor()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360, null, Encargos());

        Assert.True(resultado.Parcelas[^1].SeguroMip < resultado.Parcelas[0].SeguroMip);
        // Primeira parcela: 0,025% de 400.000 = 100,00.
        Assert.Equal(100m, resultado.Parcelas[0].SeguroMip);
    }

    [Fact]
    public void Calcular_DfiEConstantePorqueIncideSobreOImovel()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360, null, Encargos());

        // 0,01% de 500.000 = 50,00, todo mês.
        Assert.All(resultado.Parcelas, p => Assert.Equal(50m, p.SeguroDfi));
    }

    [Fact]
    public void Calcular_TaxaDeAdministracaoEFixaTodoMes()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360, null, Encargos());

        Assert.All(resultado.Parcelas, p => Assert.Equal(25m, p.TaxaAdministracao));
        Assert.Equal(25m * resultado.PrazoFinalMeses, resultado.TotalTaxaAdministracao);
    }

    [Fact]
    public void Calcular_TotaisSomamAsLinhasDoCronograma()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360, null, Encargos());

        Assert.Equal(resultado.Parcelas.Sum(p => p.Seguros), resultado.TotalSeguros);
        Assert.Equal(resultado.Parcelas.Sum(p => p.TaxaAdministracao), resultado.TotalTaxaAdministracao);
    }

    [Fact]
    public void Calcular_TotalDesembolsadoIncluiEncargosAlemDoTotalPago()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360, null, Encargos());

        Assert.Equal(
            resultado.TotalPago + resultado.TotalSeguros + resultado.TotalTaxaAdministracao,
            resultado.TotalDesembolsado);
        Assert.True(resultado.TotalDesembolsado > resultado.TotalPago);
    }

    [Fact]
    public void Calcular_ParcelaTotalSomaContratualExtrasEEncargos()
    {
        var extra = new AmortizacaoExtra(500m, null, ModoAmortizacaoExtra.ReduzirPrazo);
        var resultado = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360, extra, Encargos());

        var primeira = resultado.Parcelas[0];
        Assert.Equal(
            primeira.ValorParcela + primeira.AmortizacaoExtra + primeira.SeguroMip + primeira.SeguroDfi + primeira.TaxaAdministracao,
            primeira.ParcelaTotal);
    }

    [Fact]
    public void Calcular_SemEncargos_NaoCobraNadaAMais()
    {
        var resultado = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360);

        Assert.Equal(0m, resultado.TotalSeguros);
        Assert.Equal(0m, resultado.TotalTaxaAdministracao);
        Assert.Equal(resultado.TotalPago, resultado.TotalDesembolsado);
    }

    [Fact]
    public void Calcular_SemValorDoImovel_NaoCobraDfi()
    {
        // Quem simula informando só o valor financiado não tem base para o DFI.
        var resultado = FinanciamentoSacCalculator.Calcular(
            400000m, 0.8m, 360, null, Encargos(imovel: 0m));

        Assert.All(resultado.Parcelas, p => Assert.Equal(0m, p.SeguroDfi));
    }

    [Fact]
    public void Calcular_EncargosValemParaOsDoisSistemas()
    {
        var price = FinanciamentoPriceCalculator.Calcular(400000m, 0.8m, 360, null, Encargos());
        var sac = FinanciamentoSacCalculator.Calcular(400000m, 0.8m, 360, null, Encargos());

        Assert.True(price.TotalSeguros > 0);
        Assert.True(sac.TotalSeguros > 0);
        // O SAC amortiza mais rápido, então o MIP sobre o saldo custa menos.
        Assert.True(sac.TotalSeguros < price.TotalSeguros);
    }

    [Theory]
    [InlineData(-0.01, 0, 0)]
    [InlineData(0, -0.01, 0)]
    [InlineData(0, 0, -1)]
    public void Calcular_ComEncargoNegativo_ThrowsArgumentException(decimal mip, decimal dfi, decimal taxa)
    {
        Assert.Throws<ArgumentException>(() => FinanciamentoSacCalculator.Calcular(
            400000m, 0.8m, 360, null, new EncargosFinanciamento(mip, dfi, 500000m, taxa)));
    }
}
