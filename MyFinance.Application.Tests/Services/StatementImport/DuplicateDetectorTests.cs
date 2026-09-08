using MyFinance.Application.Dtos;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Services.StatementImport;

namespace MyFinance.Application.Tests.Services.StatementImport;

public class DuplicateDetectorTests
{
    private static readonly DateTime Dia = new(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Mark_SemHistorico_NaoMarcaNada()
    {
        var transactions = new List<AiTransactionResponseDto> { Importada("IFD*IFOOD CLUB", -5.95m) };

        Assert.Equal(0, DuplicateDetector.Mark(transactions, Array.Empty<TransactionDigest>()));
        Assert.False(transactions[0].IsDuplicate);
    }

    [Fact]
    public void Mark_LancamentoIgualJaSalvo_EhMarcado()
    {
        var transactions = new List<AiTransactionResponseDto> { Importada("IFD*IFOOD CLUB", -5.95m) };

        var marcadas = DuplicateDetector.Mark(transactions, new[] { Salva("IFD*IFOOD CLUB", -5.95m) });

        Assert.Equal(1, marcadas);
        Assert.True(transactions[0].IsDuplicate);
    }

    [Fact]
    public void Mark_DescricaoComFormatacaoDiferente_AindaEhAMesma()
    {
        // O mesmo gasto sai do CSV e do PDF com espaçamento e sufixo de praça
        // diferentes; a normalização é o que faz os dois casarem.
        var transactions = new List<AiTransactionResponseDto> { Importada("IFD IFOOD CLUB   Osasco   BRA", -5.95m) };

        var marcadas = DuplicateDetector.Mark(transactions, new[] { Salva("IFD*IFOOD CLUB Osasco BRA", -5.95m) });

        Assert.Equal(1, marcadas);
    }

    [Fact]
    public void Mark_DuasLinhasIguaisNoArquivoEUmaNoBanco_MarcaSomenteUma()
    {
        // Reimportação marca uma; a outra é uma compra repetida de verdade.
        var transactions = new List<AiTransactionResponseDto>
        {
            Importada("CAFE DA ESQUINA", -8.00m),
            Importada("CAFE DA ESQUINA", -8.00m)
        };

        var marcadas = DuplicateDetector.Mark(transactions, new[] { Salva("CAFE DA ESQUINA", -8.00m) });

        Assert.Equal(1, marcadas);
        Assert.True(transactions[0].IsDuplicate);
        Assert.False(transactions[1].IsDuplicate);
    }

    [Fact]
    public void Mark_DuasLinhasIguaisNoArquivoEDuasNoBanco_MarcaAsDuas()
    {
        var transactions = new List<AiTransactionResponseDto>
        {
            Importada("CAFE DA ESQUINA", -8.00m),
            Importada("CAFE DA ESQUINA", -8.00m)
        };

        var marcadas = DuplicateDetector.Mark(
            transactions, new[] { Salva("CAFE DA ESQUINA", -8.00m), Salva("CAFE DA ESQUINA", -8.00m) });

        Assert.Equal(2, marcadas);
    }

    [Fact]
    public void Mark_ValorDiferente_NaoEhDuplicata()
    {
        var transactions = new List<AiTransactionResponseDto> { Importada("POSTO COELHO LTDA", -258.81m) };

        Assert.Equal(0, DuplicateDetector.Mark(transactions, new[] { Salva("POSTO COELHO LTDA", -42.47m) }));
    }

    [Fact]
    public void Mark_DataDiferente_NaoEhDuplicata()
    {
        var transactions = new List<AiTransactionResponseDto> { Importada("CRUZEIROEC", -40.00m) };

        var salva = new TransactionDigest(Dia.AddDays(-1), -40.00m, "CRUZEIROEC");

        Assert.Equal(0, DuplicateDetector.Mark(transactions, new[] { salva }));
    }

    [Fact]
    public void Mark_IgnoraAHoraDaTransacaoSalva()
    {
        // Transações salvas carregam hora; as lidas do extrato, não.
        var transactions = new List<AiTransactionResponseDto> { Importada("MERCADO", -20m) };
        var salva = new TransactionDigest(Dia.AddHours(14), -20m, "MERCADO");

        Assert.Equal(1, DuplicateDetector.Mark(transactions, new[] { salva }));
    }

    [Fact]
    public void DateRange_UsaAMenorEAMaiorDataDoLote()
    {
        var entries = new[]
        {
            new ParsedStatementEntry(new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc), "A", -1m),
            new ParsedStatementEntry(new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), "B", -1m),
            new ParsedStatementEntry(new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc), "C", -1m)
        };

        var (from, to) = DuplicateDetector.DateRange(entries);

        Assert.Equal(new DateTime(2026, 7, 26), from);
        Assert.Equal(new DateTime(2026, 8, 30), to);
    }

    private static AiTransactionResponseDto Importada(string description, decimal amount) =>
        new() { Date = Dia, Description = description, Amount = amount, AccountId = Guid.NewGuid() };

    private static TransactionDigest Salva(string description, decimal amount) =>
        new(Dia, amount, description);
}
