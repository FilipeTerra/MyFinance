using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services.StatementImport;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Tests.Services.StatementImport;

public class StatementImportServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ICategoryRuleRepository> _categoryRuleRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IAiIntegrationService> _ai = new();
    private readonly Mock<IPdfTextExtractor> _pdfTextExtractor = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Category _supermercado;

    public StatementImportServiceTests()
    {
        _supermercado = new Category("Supermercado", _userId);

        _categoryRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(new[] { _supermercado });
        _categoryRuleRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(Array.Empty<CategoryRule>());
        _transactionRepository.Setup(r => r.GetDescriptionCategoryHistoryAsync(_userId))
            .ReturnsAsync(Array.Empty<DescriptionCategoryCount>());
        _transactionRepository.Setup(r => r.GetDigestsForDuplicateCheckAsync(
                _accountId, _userId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<TransactionDigest>());
        _pdfTextExtractor.Setup(e => e.ExtractLines(It.IsAny<byte[]>()))
            .Returns(StatementFixtures.InterPdfLines());
    }

    // ---------- Agente de IA fora do ar ----------

    [Fact]
    public async Task ImportAsync_ImportaCsvComOAgenteForaDoAr()
    {
        AgenteForaDoAr();

        var result = await BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

        Assert.True(result.Success);
        Assert.Equal(10, result.Transactions.Count);
        Assert.Equal("Inter CSV", result.ParserUsed);
        Assert.False(result.AiUsed);
    }

    [Fact]
    public async Task ImportAsync_ImportaPdfComOAgenteForaDoAr()
    {
        AgenteForaDoAr();

        var result = await BuildSut().ImportAsync(StatementFixtures.Pdf(), _accountId, _userId);

        Assert.True(result.Success);
        Assert.Equal(3, result.Transactions.Count);
        Assert.Equal("Inter PDF", result.ParserUsed);
    }

    [Fact]
    public async Task ImportAsync_NaoChamaOAgenteQuandoOParserDaConta()
    {
        AgenteNoAr();
        _ai.Setup(a => a.SuggestCategoriesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(new Dictionary<string, string>());

        await BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

        _ai.Verify(a => a.ExtractStatementAsync(It.IsAny<StatementFile>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_AvisaQuandoOAgenteEstaForaEHaLinhasSemCategoria()
    {
        AgenteForaDoAr();

        var result = await BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

        Assert.True(result.AiUnavailable);
        Assert.NotEmpty(result.Warnings);
        Assert.NotEmpty(CategoryResolver.Unresolved(result.Transactions));
    }

    // ---------- Formato desconhecido ----------

    [Fact]
    public async Task ImportAsync_UsaOAgenteQuandoNenhumParserReconhece()
    {
        AgenteNoAr();
        _ai.Setup(a => a.ExtractStatementAsync(It.IsAny<StatementFile>()))
            .ReturnsAsync(new[]
            {
                new ParsedStatementEntry(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), "ALGO", -10m)
            });
        _ai.Setup(a => a.SuggestCategoriesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await BuildSut().ImportAsync(FormatoDesconhecido(), _accountId, _userId);

        Assert.True(result.Success);
        Assert.True(result.AiUsed);
        Assert.Equal("IA", result.ParserUsed);
    }

    [Fact]
    public async Task ImportAsync_FalhaComMensagemClaraQuandoOFormatoEDesconhecidoEOAgenteEstaFora()
    {
        AgenteForaDoAr();

        var result = await BuildSut().ImportAsync(FormatoDesconhecido(), _accountId, _userId);

        Assert.False(result.Success);
        Assert.True(result.AiUnavailable);
        Assert.Contains("não é reconhecido", result.Message);
        Assert.Empty(result.Transactions);
    }

    // ---------- Duplicatas ----------

    [Fact]
    public async Task ImportAsync_SemHistorico_NaoMarcaDuplicata()
    {
        AgenteForaDoAr();

        var result = await BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

        Assert.Equal(0, result.DuplicateCount);
        Assert.All(result.Transactions, t => Assert.False(t.IsDuplicate));
    }

    [Fact]
    public async Task ImportAsync_ReimportarOMesmoArquivoMarcaTodasAsLinhas()
    {
        AgenteForaDoAr();
        var jaSalvas = (await ImportarUmaVez()).Transactions
            .Select(t => new TransactionDigest(t.Date, t.Amount, t.Description))
            .ToList();

        _transactionRepository.Setup(r => r.GetDigestsForDuplicateCheckAsync(
                _accountId, _userId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(jaSalvas);

        var result = await BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

        Assert.Equal(result.Transactions.Count, result.DuplicateCount);
        Assert.All(result.Transactions, t => Assert.True(t.IsDuplicate));
    }

    [Fact]
    public async Task ImportAsync_ConsultaApenasAFaixaDeDatasDoArquivo()
    {
        AgenteForaDoAr();

        await BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

        // O fixture vai de 26/07 a 30/08; varrer a conta inteira seria desperdício.
        _transactionRepository.Verify(r => r.GetDigestsForDuplicateCheckAsync(
            _accountId, _userId,
            new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc)), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_FalhaNaChecagemDeDuplicataNaoDerrubaAImportacao()
    {
        AgenteForaDoAr();
        _transactionRepository.Setup(r => r.GetDigestsForDuplicateCheckAsync(
                _accountId, _userId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new InvalidOperationException("banco indisponível"));

        var result = await BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

        Assert.True(result.Success);
        Assert.Equal(10, result.Transactions.Count);
        Assert.Equal(0, result.DuplicateCount);
    }

    // ---------- Enriquecimento de categorias ----------

    [Fact]
    public async Task ImportAsync_AplicaSugestaoDaIaSomenteNoQueSobrouSemCategoria()
    {
        AgenteNoAr();
        _ai.Setup(a => a.SuggestCategoriesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(new Dictionary<string, string> { ["PAGAMENTO ON LINE"] = "Supermercado" });

        var result = await BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

        var pagamento = result.Transactions.Single(t => t.Description == "PAGAMENTO ON LINE");
        Assert.True(pagamento.IsSuggestion);
        Assert.Equal("Supermercado", pagamento.SuggestedCategoryName);
        Assert.True(result.AiUsed);
    }

    [Fact]
    public async Task ImportAsync_FalhaNaSugestaoNaoDerrubaAImportacao()
    {
        AgenteNoAr();
        _ai.Setup(a => a.SuggestCategoriesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>?)null);

        var result = await BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

        Assert.True(result.Success);
        Assert.Equal(10, result.Transactions.Count);
        Assert.True(result.AiUnavailable);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task ImportAsync_ParserQueEstouraNaoDerrubaAImportacao()
    {
        AgenteForaDoAr();
        _pdfTextExtractor.Setup(e => e.ExtractLines(It.IsAny<byte[]>()))
            .Throws(new InvalidOperationException("PDF criptografado"));

        var result = await BuildSut().ImportAsync(StatementFixtures.Pdf(), _accountId, _userId);

        Assert.False(result.Success);
        Assert.Contains("não é reconhecido", result.Message);
    }

    // ---------- Helpers ----------

    private void AgenteForaDoAr() => _ai.Setup(a => a.IsAvailableAsync()).ReturnsAsync(false);

    /// <summary>Primeira importação do fixture, para servir de "histórico já salvo".</summary>
    private Task<StatementImportResultDto> ImportarUmaVez() =>
        BuildSut().ImportAsync(StatementFixtures.InterCsv(), _accountId, _userId);

    private void AgenteNoAr() => _ai.Setup(a => a.IsAvailableAsync()).ReturnsAsync(true);

    private static StatementFile FormatoDesconhecido() =>
        StatementFixtures.FromText("relatório sem estrutura tabular alguma", "relatorio.txt");

    private StatementImportService BuildSut()
    {
        var parsers = new IStatementParser[]
        {
            new InterCsvStatementParser(),
            new InterPdfStatementParser(_pdfTextExtractor.Object),
            new GenericCsvStatementParser()
        };

        return new StatementImportService(
            parsers,
            _categoryRepository.Object,
            _categoryRuleRepository.Object,
            _transactionRepository.Object,
            _ai.Object,
            NullLogger<StatementImportService>.Instance);
    }
}
