using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class ComposicaoFinanciamentoTests
{
    [Fact]
    public void Resolver_WithEntradaInReais_SubtractsItFromThePropertyPrice()
    {
        var composicao = ComposicaoFinanciamento.Resolver(500000m, 100000m, null);

        Assert.Equal(400000m, composicao.ValorFinanciado);
        Assert.Equal(100000m, composicao.Entrada);
        Assert.Equal(20m, composicao.EntradaPercentual);
    }

    [Fact]
    public void Resolver_WithEntradaAsPercentage_ConvertsItToReais()
    {
        var composicao = ComposicaoFinanciamento.Resolver(500000m, null, 20m);

        Assert.Equal(100000m, composicao.Entrada);
        Assert.Equal(400000m, composicao.ValorFinanciado);
    }

    [Fact]
    public void Resolver_WhenBothFormsAreGiven_PrefersTheValueInReais()
    {
        // O campo em reais é o que o usuário digitou por último na tela; o
        // percentual fica como fallback para quem raciocina em "20% de entrada".
        var composicao = ComposicaoFinanciamento.Resolver(500000m, 150000m, 20m);

        Assert.Equal(150000m, composicao.Entrada);
        Assert.Equal(30m, composicao.EntradaPercentual);
    }

    [Fact]
    public void Resolver_WithoutEntrada_FinancesTheWholeProperty()
    {
        var composicao = ComposicaoFinanciamento.Resolver(300000m, null, null);

        Assert.Equal(0m, composicao.Entrada);
        Assert.Equal(300000m, composicao.ValorFinanciado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Resolver_WithInvalidPropertyPrice_ThrowsArgumentException(decimal valorImovel)
    {
        Assert.Throws<ArgumentException>(() => ComposicaoFinanciamento.Resolver(valorImovel, null, null));
    }

    [Fact]
    public void Resolver_WithEntradaCoveringTheWholeProperty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ComposicaoFinanciamento.Resolver(300000m, 300000m, null));
    }

    [Fact]
    public void Resolver_WithNegativeEntrada_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ComposicaoFinanciamento.Resolver(300000m, -1m, null));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(101)]
    public void Resolver_WithPercentageOutsideRange_ThrowsArgumentException(decimal percentual)
    {
        Assert.Throws<ArgumentException>(() => ComposicaoFinanciamento.Resolver(300000m, null, percentual));
    }
}
