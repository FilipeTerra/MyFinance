using Moq;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services.StatementImport;

namespace MyFinance.Application.Tests.Services.StatementImport;

public class InterPdfStatementParserTests
{
    private readonly Mock<IPdfTextExtractor> _pdfTextExtractor = new();
    private readonly InterPdfStatementParser _sut;

    public InterPdfStatementParserTests()
    {
        _sut = new InterPdfStatementParser(_pdfTextExtractor.Object);
        _pdfTextExtractor.Setup(e => e.ExtractLines(It.IsAny<byte[]>()))
            .Returns(StatementFixtures.InterPdfLines());
    }

    [Fact]
    public void CanParse_AceitaSomentePdf()
    {
        Assert.True(_sut.CanParse(StatementFixtures.Pdf()));
        Assert.False(_sut.CanParse(StatementFixtures.InterCsv()));
    }

    [Fact]
    public void Parse_IgnoraResumoCabecalhosETotais()
    {
        var entries = _sut.Parse(StatementFixtures.Pdf());

        // As páginas de resumo/parcelamento, os cabeçalhos de tabela, os títulos
        // de cartão e as linhas "Total CARTÃO" também têm valores em reais e não
        // podem virar transação.
        Assert.Equal(3, entries.Count);
    }

    [Fact]
    public void Parse_LeDataPorExtenso()
    {
        var uber = _sut.Parse(StatementFixtures.Pdf()).Single(e => e.Description == "DL*UberRides");

        Assert.Equal(new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc), uber.Date);
        Assert.Equal(-37.95m, uber.Amount);
    }

    [Fact]
    public void Parse_ValorComMaisEhReceita()
    {
        var pagamento = _sut.Parse(StatementFixtures.Pdf())
            .Single(e => e.Description == "PAGAMENTO ON LINE");

        Assert.Equal(4657.32m, pagamento.Amount);
    }

    [Fact]
    public void Parse_MantemAParcelaNaDescricao()
    {
        var parcela = _sut.Parse(StatementFixtures.Pdf())
            .Single(e => e.Description.StartsWith("CP PARC DUO"));

        Assert.Equal("CP PARC DUO GOURMET (Parcela 06 de 09)", parcela.Description);
        Assert.Equal(-59.78m, parcela.Amount);
    }

    [Fact]
    public void Parse_NaoDevolveNadaQuandoNaoHaMarcadorDeDespesas()
    {
        _pdfTextExtractor.Setup(e => e.ExtractLines(It.IsAny<byte[]>()))
            .Returns(new List<PdfLine>
            {
                new(1, new List<PdfWord> { new("Comprovante", 40, 100), new("de", 105, 120) })
            });

        Assert.Empty(_sut.Parse(StatementFixtures.Pdf()));
    }

    [Fact]
    public void Parse_NaoDevolveNadaQuandoOPdfNaoTemTexto()
    {
        _pdfTextExtractor.Setup(e => e.ExtractLines(It.IsAny<byte[]>()))
            .Returns(new List<PdfLine>());

        Assert.Empty(_sut.Parse(StatementFixtures.Pdf()));
    }
}
