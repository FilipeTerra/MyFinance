using MyFinance.Application.Services.StatementImport;

namespace MyFinance.Application.Tests.Services.StatementImport;

public class GenericCsvStatementParserTests
{
    private readonly GenericCsvStatementParser _sut = new();

    [Fact]
    public void Parse_LeCsvComPontoEVirgulaEOutrosCabecalhos()
    {
        var file = StatementFixtures.FromText(
            "Data;Histórico;Valor\n" +
            "05/03/2026;SUPERMERCADO CENTRAL;-89,90\n" +
            "06/03/2026;SALARIO;3.500,00\n");

        var entries = _sut.Parse(file);

        Assert.Equal(2, entries.Count);
        Assert.Equal(-89.90m, entries[0].Amount);
        Assert.Equal(3500.00m, entries[1].Amount);
    }

    [Fact]
    public void Parse_UsaOCampoTipoQuandoOValorNaoTemSinal()
    {
        var file = StatementFixtures.FromText(
            "Date;Description;Amount;Type\n" +
            "2026-03-07;ESTORNO COMPRA;42,10;Estorno\n");

        var entry = Assert.Single(_sut.Parse(file));

        Assert.Equal(42.10m, entry.Amount);
    }

    [Fact]
    public void Parse_IgnoraLinhaSemDataValida()
    {
        var file = StatementFixtures.FromText(
            "Data;Descrição;Valor\n" +
            "saldo anterior;;-10,00\n" +
            "05/03/2026;PADARIA;-10,00\n");

        Assert.Single(_sut.Parse(file));
    }

    [Fact]
    public void CanParse_RejeitaArquivoSemColunaDeValor()
    {
        var file = StatementFixtures.FromText("Data;Descrição\n05/03/2026;PADARIA\n");

        Assert.False(_sut.CanParse(file));
    }

    [Fact]
    public void CanParse_NaoDisputaComOsParsersEspecificos()
    {
        // Prioridade maior significa "tentado por último": o parser específico do
        // Inter precisa vencer no arquivo dele.
        Assert.True(_sut.Priority > new InterCsvStatementParser().Priority);
    }
}
