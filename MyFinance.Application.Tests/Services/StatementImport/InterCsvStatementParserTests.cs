using MyFinance.Application.Services.StatementImport;

namespace MyFinance.Application.Tests.Services.StatementImport;

public class InterCsvStatementParserTests
{
    private readonly InterCsvStatementParser _sut = new();

    [Fact]
    public void CanParse_ReconheceOCsvDoInter()
    {
        Assert.True(_sut.CanParse(StatementFixtures.InterCsv()));
    }

    [Fact]
    public void CanParse_RejeitaPdf()
    {
        Assert.False(_sut.CanParse(StatementFixtures.Pdf()));
    }

    [Fact]
    public void CanParse_RejeitaCsvSemAsColunasObrigatorias()
    {
        var file = StatementFixtures.FromText("\"Dia\",\"Texto\"\n\"01/01/2026\",\"Algo\"");

        Assert.False(_sut.CanParse(file));
    }

    [Fact]
    public void Parse_LeTodasAsLinhasDoArquivo()
    {
        var entries = _sut.Parse(StatementFixtures.InterCsv());

        Assert.Equal(10, entries.Count);
    }

    [Fact]
    public void Parse_TrataCompraComoDespesa()
    {
        var compra = _sut.Parse(StatementFixtures.InterCsv())
            .Single(e => e.Description.StartsWith("IFD*IFOOD"));

        Assert.Equal(-5.95m, compra.Amount);
        Assert.Equal(new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), compra.Date);
    }

    [Fact]
    public void Parse_TrataValorNegativoComoReceita()
    {
        // Numa fatura, "-R$ 150,00" é o pagamento entrando, não um gasto.
        var pagamento = _sut.Parse(StatementFixtures.InterCsv())
            .Single(e => e.Description == "PAGAMENTO ON LINE");

        Assert.Equal(150.00m, pagamento.Amount);
    }

    [Fact]
    public void Parse_EntendeSeparadorDeMilhar()
    {
        var viagem = _sut.Parse(StatementFixtures.InterCsv())
            .Single(e => e.Description.StartsWith("Zupper"));

        Assert.Equal(-1076.26m, viagem.Amount);
    }

    [Fact]
    public void Parse_UsaACategoriaDoArquivoEmTitleCase()
    {
        var drogaria = _sut.Parse(StatementFixtures.InterCsv())
            .Single(e => e.Description.StartsWith("DROGARIA"));

        Assert.Equal("Drogaria", drogaria.FileCategoryName);
    }

    [Fact]
    public void Parse_DescartaCategoriaGenericaDoBanco()
    {
        var iof = _sut.Parse(StatementFixtures.InterCsv())
            .Single(e => e.Description == "IOF INTERNACIONAL");

        Assert.Null(iof.FileCategoryName);
        Assert.Equal(-3.85m, iof.Amount);
    }

    [Fact]
    public void Parse_AceitaArquivoEmIso88591()
    {
        var conteudo = "\"Data\",\"Lançamento\",\"Valor\"\n\"05/03/2026\",\"PADARIA SÃO JOÃO\",\"R$ 10,00\"";
        var file = new Application.Dtos.StatementImport.StatementFile(
            "extrato.csv", "text/csv", System.Text.Encoding.Latin1.GetBytes(conteudo));

        var entry = Assert.Single(_sut.Parse(file));

        Assert.Equal("PADARIA SÃO JOÃO", entry.Description);
    }
}
