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

    [Fact]
    public void Resolver_WithoutAcquisitionCosts_DesembolsoInicialIsJustTheEntrada()
    {
        var composicao = ComposicaoFinanciamento.Resolver(500000m, 100000m, null);

        Assert.Equal(100000m, composicao.DesembolsoInicial);
    }

    [Fact]
    public void Resolver_WithItbiAndCustosCartorio_DesembolsoInicialSumsEntradaAndAcquisitionCosts()
    {
        var composicao = ComposicaoFinanciamento.Resolver(500000m, 100000m, null, itbi: 15000m, custosCartorio: 4000m);

        Assert.Equal(15000m, composicao.Itbi);
        Assert.Equal(4000m, composicao.CustosCartorio);
        Assert.Equal(119000m, composicao.DesembolsoInicial);
        // Custos de fechamento não entram no financiado — só a entrada abate o imóvel.
        Assert.Equal(400000m, composicao.ValorFinanciado);
    }

    [Fact]
    public void Resolver_WithNegativeItbi_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ComposicaoFinanciamento.Resolver(300000m, null, null, itbi: -1m));
    }

    [Fact]
    public void Resolver_WithNegativeCustosCartorio_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ComposicaoFinanciamento.Resolver(300000m, null, null, custosCartorio: -1m));
    }

    [Fact]
    public void Resolver_WithSubsidio_SubtractsItFromFinanciadoLikeAnEntrada()
    {
        var composicao = ComposicaoFinanciamento.Resolver(500000m, 50000m, null, subsidio: 40000m);

        Assert.Equal(40000m, composicao.Subsidio);
        Assert.Equal(410000m, composicao.ValorFinanciado);
    }

    [Fact]
    public void Resolver_WithSubsidio_DoesNotChangeEntradaPercentual()
    {
        // O subsídio é do governo, não do comprador — não conta como parte da entrada dele.
        var composicao = ComposicaoFinanciamento.Resolver(500000m, 50000m, null, subsidio: 40000m);

        Assert.Equal(10m, composicao.EntradaPercentual);
    }

    [Fact]
    public void Resolver_WithSubsidio_DoesNotAffectDesembolsoInicial()
    {
        var composicao = ComposicaoFinanciamento.Resolver(500000m, 50000m, null, subsidio: 40000m);

        Assert.Equal(50000m, composicao.DesembolsoInicial);
    }

    [Fact]
    public void Resolver_WithNegativeSubsidio_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ComposicaoFinanciamento.Resolver(300000m, null, null, subsidio: -1m));
    }

    [Fact]
    public void Resolver_WithEntradaAndSubsidioCoveringTheWholeProperty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ComposicaoFinanciamento.Resolver(300000m, 150000m, null, subsidio: 150000m));
    }
}
