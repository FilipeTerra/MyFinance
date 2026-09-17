using MyFinance.Application.Services.StatementImport;

namespace MyFinance.Application.Tests.Services.StatementImport;

public class InterExtratoCsvStatementParserTests
{
    private readonly InterExtratoCsvStatementParser _sut = new();

    [Fact]
    public void CanParse_ReconheceOExtratoDeContaCorrenteDoInter()
    {
        Assert.True(_sut.CanParse(StatementFixtures.InterExtratoCsv()));
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
    public void CanParse_RejeitaCabecalhoAlemDoLimiteDeBusca()
    {
        var preambulo = string.Concat(Enumerable.Repeat("Linha de metadado sem relação\n", 25));
        var file = StatementFixtures.FromText(
            preambulo + "Data Lançamento;Descrição;Valor;Saldo\n05/01/2026;PADARIA;-10,00;100,00\n");

        Assert.False(_sut.CanParse(file));
    }

    [Fact]
    public void CanParse_PulaLinhasDeMetadadoAntesDoCabecalho()
    {
        var file = StatementFixtures.FromText(
            "Extrato Conta Corrente\n" +
            "Conta ;99999999\n" +
            "Período ;01/01/2026 a 31/01/2026\n" +
            "Saldo: ;0,00\n\n" +
            "Data Lançamento;Descrição;Valor;Saldo\n" +
            "05/01/2026;PADARIA;-10,00;100,00\n");

        Assert.True(_sut.CanParse(file));
    }

    [Fact]
    public void Parse_LeTodasAsLinhasDoArquivo()
    {
        var entries = _sut.Parse(StatementFixtures.InterExtratoCsv());

        Assert.Equal(6, entries.Count);
    }

    [Fact]
    public void Parse_TrataPixRecebidoComoReceita()
    {
        var pix = _sut.Parse(StatementFixtures.InterExtratoCsv())
            .Single(e => e.Description.StartsWith("Pix recebido"));

        Assert.Equal(1076.26m, pix.Amount);
        Assert.Equal(new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), pix.Date);
    }

    [Fact]
    public void Parse_TrataPixEnviadoComoDespesa()
    {
        var pix = _sut.Parse(StatementFixtures.InterExtratoCsv())
            .Single(e => e.Description.StartsWith("Pix enviado"));

        Assert.Equal(-50.00m, pix.Amount);
    }

    [Fact]
    public void Parse_TrataBoletoRecebidoComoReceita()
    {
        var boleto = _sut.Parse(StatementFixtures.InterExtratoCsv())
            .Single(e => e.Description.StartsWith("Boleto recebido"));

        Assert.Equal(600.00m, boleto.Amount);
    }

    [Fact]
    public void Parse_TrataSaqueComoDespesa()
    {
        var saque = _sut.Parse(StatementFixtures.InterExtratoCsv())
            .Single(e => e.Description.StartsWith("SAQUE"));

        Assert.Equal(-200.00m, saque.Amount);
    }

    [Fact]
    public void Parse_EntendeSeparadorDeMilhar()
    {
        var pix = _sut.Parse(StatementFixtures.InterExtratoCsv())
            .Single(e => e.Description.StartsWith("Pix recebido"));

        Assert.Equal(1076.26m, pix.Amount);
    }

    [Fact]
    public void Parse_NaoAtribuiCategoriaDeArquivo()
    {
        var entries = _sut.Parse(StatementFixtures.InterExtratoCsv());

        Assert.All(entries, e => Assert.Null(e.FileCategoryName));
    }

    [Fact]
    public void Parse_AceitaArquivoEmIso88591()
    {
        var conteudo =
            "Extrato Conta Corrente\n" +
            "Conta ;12345678\n" +
            "Período ;01/01/2026 a 31/01/2026\n" +
            "Saldo: ;0,00\n\n" +
            "Data Lançamento;Descrição;Valor;Saldo\n" +
            "05/03/2026;PADARIA SÃO JOÃO;-10,00;90,00\n";
        var file = new Application.Dtos.StatementImport.StatementFile(
            "extrato.csv", "text/csv", System.Text.Encoding.Latin1.GetBytes(conteudo));

        var entry = Assert.Single(_sut.Parse(file));

        Assert.Equal("PADARIA SÃO JOÃO", entry.Description);
        Assert.Equal(-10.00m, entry.Amount);
    }

    [Fact]
    public void Priority_FicaEntreAFaturaCsvEOGenerico()
    {
        Assert.True(_sut.Priority > new InterCsvStatementParser().Priority);
        Assert.True(_sut.Priority < new GenericCsvStatementParser().Priority);
    }
}
