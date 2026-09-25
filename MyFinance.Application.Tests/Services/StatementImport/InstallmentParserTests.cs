using MyFinance.Application.Services.StatementImport;

namespace MyFinance.Application.Tests.Services.StatementImport;

public class InstallmentParserTests
{
    [Theory]
    // Descrição do PDF do Inter.
    [InlineData("CP PARC DUO GOURMET (Parcela 06 de 09)", 6, 9)]
    [InlineData("CENTAURO.COM (Parcela 03 de 10)", 3, 10)]
    // Coluna "Tipo" do CSV do Inter.
    [InlineData("Parcela 2/3", 2, 3)]
    [InlineData("PARCELA 12 DE 12", 12, 12)]
    // Abreviações e variações de outros bancos.
    [InlineData("LOJA X PARC 02/12", 2, 12)]
    [InlineData("NETFLIX - Parcelas 4/6", 4, 6)]
    [InlineData("COMPRA ALGUMA (2/12)", 2, 12)]
    // Campo que só contém a fração.
    [InlineData("2/3", 2, 3)]
    [InlineData(" 03 de 10 ", 3, 10)]
    public void TryParse_ReconheceOsFormatosDeParcela(string texto, int numeroEsperado, int totalEsperado)
    {
        Assert.True(InstallmentParser.TryParse(texto, out var numero, out var total));
        Assert.Equal(numeroEsperado, numero);
        Assert.Equal(totalEsperado, total);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("IFD*IFOOD CLUB")]
    [InlineData("Compra à vista")]
    [InlineData("PAGAMENTO DE FATURA")]
    public void TryParse_SemMarcacaoDeParcelaNaoReconhece(string? texto)
    {
        Assert.False(InstallmentParser.TryParse(texto, out _, out _));
    }

    [Theory]
    // Um "02/12" solto é indistinguível de data — ler como parcela inventaria dívida.
    [InlineData("ZUPPER 02/12 SAO PAULO")]
    [InlineData("COMPRA EM 30/08")]
    public void TryParse_IgnoraFracaoSoltaNoMeioDaDescricao(string texto)
    {
        Assert.False(InstallmentParser.TryParse(texto, out _, out _));
    }

    [Theory]
    [InlineData("Parcela 7 de 3")]   // parcela além do total
    [InlineData("Parcela 0 de 5")]   // parcela zero não existe
    [InlineData("Parcela 1 de 0")]   // total zerado
    public void TryParse_RejeitaParcelamentoIncoerente(string texto)
    {
        Assert.False(InstallmentParser.TryParse(texto, out var numero, out var total));
        Assert.Equal(0, numero);
        Assert.Equal(0, total);
    }

    [Fact]
    public void RemoveFrom_ApagaOSufixoComOsParenteses()
    {
        Assert.Equal("CP PARC DUO GOURMET", InstallmentParser.RemoveFrom("CP PARC DUO GOURMET (Parcela 06 de 09)").Trim());
    }

    [Fact]
    public void RemoveFrom_ApagaOSufixoSemParenteses()
    {
        Assert.Equal("NETFLIX", InstallmentParser.RemoveFrom("NETFLIX Parcela 2/3").Trim());
    }

    [Fact]
    public void RemoveFrom_PreservaTextoSemParcela()
    {
        Assert.Equal("IFD*IFOOD CLUB", InstallmentParser.RemoveFrom("IFD*IFOOD CLUB"));
    }
}
