using System.Linq;
using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class MinhaCasaMinhaVidaTabelaTests
{
    [Theory]
    [InlineData(3200.00, FaixaMinhaCasaMinhaVida.Faixa1)]
    [InlineData(3200.01, FaixaMinhaCasaMinhaVida.Faixa2)]
    [InlineData(5000.00, FaixaMinhaCasaMinhaVida.Faixa2)]
    [InlineData(5000.01, FaixaMinhaCasaMinhaVida.Faixa3)]
    [InlineData(9600.00, FaixaMinhaCasaMinhaVida.Faixa3)]
    [InlineData(9600.01, FaixaMinhaCasaMinhaVida.Faixa4)]
    [InlineData(13000.00, FaixaMinhaCasaMinhaVida.Faixa4)]
    public void ResolverFaixa_NasBordasDeRenda_ResolveAFaixaCorreta(decimal renda, FaixaMinhaCasaMinhaVida esperada)
    {
        var faixa = MinhaCasaMinhaVidaTabela.ResolverFaixa(renda);

        Assert.NotNull(faixa);
        Assert.Equal(esperada, faixa!.Faixa);
    }

    [Fact]
    public void ResolverFaixa_AcimaDoTetoDaFaixa4_RetornaNulo()
    {
        var faixa = MinhaCasaMinhaVidaTabela.ResolverFaixa(13000.01m);

        Assert.Null(faixa);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResolverFaixa_ComRendaNaoPositiva_RetornaNulo(decimal renda)
    {
        Assert.Null(MinhaCasaMinhaVidaTabela.ResolverFaixa(renda));
    }

    [Fact]
    public void TaxaTetoMensalPercentual_ConverteATaxaAnualMaximaParaMensalEquivalente()
    {
        var faixa = MinhaCasaMinhaVidaTabela.Faixas.Single(f => f.Faixa == FaixaMinhaCasaMinhaVida.Faixa1);

        var mensal = MinhaCasaMinhaVidaTabela.TaxaTetoMensalPercentual(faixa);

        // 5,25% a.a. -> mensal equivalente é (1,0525)^(1/12) - 1 ~= 0,4273% a.m.
        Assert.Equal(0.4273m, mensal, 3);
    }

    [Fact]
    public void RendaMaximaDoPrograma_EATetoDaFaixa4()
    {
        Assert.Equal(13000m, MinhaCasaMinhaVidaTabela.RendaMaximaDoPrograma);
    }
}
