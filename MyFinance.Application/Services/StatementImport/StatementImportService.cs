using Microsoft.Extensions.Logging;
using MyFinance.Application.Dtos;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Orquestra a importação de um ou vários extratos. A ordem é deliberada:
/// primeiro o que funciona sempre (parsers determinísticos e o que o usuário já
/// ensinou) e só depois o agente de IA, tratado como opcional em todas as
/// etapas — se ele estiver fora, a importação acontece do mesmo jeito.
///
/// Num lote de vários arquivos, a leitura de cada um roda em paralelo (não toca
/// banco: parsers são puros e o cliente de IA é um HttpClient sem estado). Já a
/// resolução de categoria, a checagem de duplicata e o enriquecimento por IA
/// rodam uma única vez para o lote inteiro, e não uma vez por arquivo — tanto
/// porque o contexto (categorias, regras, histórico) é o mesmo usuário e a
/// mesma conta em todos os arquivos, quanto porque os repositórios usam o
/// ApplicationDbContext desta requisição, que não é seguro para chamadas
/// concorrentes.
/// </summary>
public class StatementImportService : IStatementImportService
{
    private const string AiParserName = "IA";

    private readonly IReadOnlyList<IStatementParser> _parsers;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICategoryRuleRepository _categoryRuleRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAiIntegrationService _aiIntegrationService;
    private readonly ILogger<StatementImportService> _logger;

    /// <summary>Um arquivo processado dentro do lote: o que se leu dele, ou por que não deu.</summary>
    private sealed record FileParseOutcome(
        StatementFile File,
        IReadOnlyList<ParsedStatementEntry> Entries,
        string? ParserUsed,
        bool AiUsed,
        bool AiUnavailable,
        string? ErrorMessage);

    public StatementImportService(
        IEnumerable<IStatementParser> parsers,
        ICategoryRepository categoryRepository,
        ICategoryRuleRepository categoryRuleRepository,
        ITransactionRepository transactionRepository,
        IAiIntegrationService aiIntegrationService,
        ILogger<StatementImportService> logger)
    {
        _parsers = parsers.OrderBy(p => p.Priority).ToList();
        _categoryRepository = categoryRepository;
        _categoryRuleRepository = categoryRuleRepository;
        _transactionRepository = transactionRepository;
        _aiIntegrationService = aiIntegrationService;
        _logger = logger;
    }

    public Task<StatementImportResultDto> ImportAsync(StatementFile file, Guid accountId, Guid userId) =>
        ImportAsync(new[] { file }, accountId, userId);

    public async Task<StatementImportResultDto> ImportAsync(
        IReadOnlyList<StatementFile> files, Guid accountId, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = new StatementImportResultDto();

        if (files.Count == 0)
        {
            result.Success = false;
            result.Message = "Nenhum arquivo enviado.";
            return result;
        }

        // Uma única checagem de disponibilidade para o lote inteiro. Lazy<Task<T>>
        // garante que a fábrica roda exatamente uma vez mesmo com .Value acessado
        // por várias tarefas concorrentes — é o que evita repetir essa chamada
        // entre arquivos e entre a extração e o enriquecimento mais adiante.
        var aiAvailability = new Lazy<Task<bool>>(() => _aiIntegrationService.IsAvailableAsync());

        var outcomes = new FileParseOutcome[files.Count];
        var degreeOfParallelism = Math.Max(1, Math.Min(files.Count, Environment.ProcessorCount));

        await Parallel.ForEachAsync(
            Enumerable.Range(0, files.Count),
            new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism, CancellationToken = cancellationToken },
            async (index, ct) =>
            {
                outcomes[index] = await ParseFileAsync(files[index], aiAvailability, ct);
            });

        var successfulOutcomes = outcomes.Where(o => o.Entries.Count > 0).ToList();

        if (successfulOutcomes.Count == 0)
        {
            ApplyAllFailed(result, outcomes);
            return result;
        }

        // A partir daqui é sequencial de propósito — ver o comentário da classe.
        var context = await BuildContextAsync(userId);

        var combinedTransactions = new List<AiTransactionResponseDto>();
        var allEntries = new List<ParsedStatementEntry>();

        foreach (var outcome in outcomes)
        {
            result.Files.Add(BuildSummary(outcome));

            if (outcome.Entries.Count == 0)
            {
                result.Warnings.Add(BuildFileWarning(outcome));
                if (outcome.AiUnavailable)
                    result.AiUnavailable = true;
                continue;
            }

            var dtos = CategoryResolver.Resolve(outcome.Entries, accountId, context);
            foreach (var dto in dtos)
                dto.SourceFileName = outcome.File.FileName;

            combinedTransactions.AddRange(dtos);
            allEntries.AddRange(outcome.Entries);
            result.AiUsed = result.AiUsed || outcome.AiUsed;
        }

        result.Transactions = combinedTransactions;
        result.ParserUsed = successfulOutcomes.Count == 1 ? successfulOutcomes[0].ParserUsed : null;

        result.DuplicateCount = await MarkDuplicatesAsync(combinedTransactions, allEntries, accountId, userId);

        await EnrichWithAiAsync(result, context, aiAvailability);

        _logger.LogInformation(
            "Lote de {Total} arquivo(s) importado: {Sucesso} com sucesso, {Transacoes} transação(ões), "
            + "{SemCategoria} sem categoria, {Duplicadas} já existente(s) na conta.",
            files.Count, successfulOutcomes.Count, result.Transactions.Count,
            CategoryResolver.Unresolved(result.Transactions).Count, result.DuplicateCount);

        return result;
    }

    /// <summary>
    /// Lê um arquivo: tenta os parsers determinísticos e, se nenhum reconhecer,
    /// cai para a IA. Nunca deixa uma exceção escapar — um arquivo com problema
    /// vira um outcome de falha, para não derrubar os outros do mesmo lote que
    /// estão rodando em paralelo.
    /// </summary>
    private async Task<FileParseOutcome> ParseFileAsync(
        StatementFile file, Lazy<Task<bool>> aiAvailability, CancellationToken cancellationToken)
    {
        try
        {
            var entries = ParseDeterministically(file, out var parserName);
            if (entries.Count > 0)
                return new FileParseOutcome(file, entries, parserName, AiUsed: false, AiUnavailable: false, ErrorMessage: null);

            if (!await aiAvailability.Value)
                return Unreadable(file, aiUnavailable: true);

            cancellationToken.ThrowIfCancellationRequested();

            var aiEntries = await _aiIntegrationService.ExtractStatementAsync(file);
            if (aiEntries is null)
                return Unreadable(file, aiUnavailable: true);

            if (aiEntries.Count == 0)
                return Unreadable(file, aiUnavailable: false);

            return new FileParseOutcome(file, aiEntries.ToList(), AiParserName, AiUsed: true, AiUnavailable: false, ErrorMessage: null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha inesperada ao processar '{Arquivo}' no lote.", file.FileName);
            return Unreadable(file, aiUnavailable: false);
        }
    }

    /// <summary>
    /// Tenta os parsers na ordem de prioridade. Um parser que estoure não derruba
    /// a importação: PDF criptografado ou CSV corrompido apenas passam a vez.
    /// </summary>
    private List<ParsedStatementEntry> ParseDeterministically(StatementFile file, out string? parserName)
    {
        parserName = null;

        foreach (var parser in _parsers)
        {
            try
            {
                if (!parser.CanParse(file))
                    continue;

                var entries = parser.Parse(file).ToList();
                if (entries.Count == 0)
                {
                    _logger.LogInformation(
                        "Parser {Parser} reconheceu '{Arquivo}' mas não achou transações.", parser.Name, file.FileName);
                    continue;
                }

                parserName = parser.Name;
                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "Parser {Parser} falhou ao ler '{Arquivo}'. Tentando o próximo.", parser.Name, file.FileName);
            }
        }

        return new List<ParsedStatementEntry>();
    }

    /// <summary>
    /// Confronta o que foi lido com o que já está salvo na conta, na faixa de datas
    /// do lote inteiro (menor e maior data entre todos os arquivos). Uma falha
    /// aqui não pode impedir a importação: no pior caso o usuário revisa sem o
    /// aviso de duplicata, que é o comportamento que existia antes.
    /// </summary>
    private async Task<int> MarkDuplicatesAsync(
        List<AiTransactionResponseDto> transactions,
        IReadOnlyList<ParsedStatementEntry> entries,
        Guid accountId,
        Guid userId)
    {
        try
        {
            var (from, to) = DuplicateDetector.DateRange(entries);
            var existing = await _transactionRepository.GetDigestsForDuplicateCheckAsync(
                accountId, userId, from, to);

            return DuplicateDetector.Mark(transactions, existing);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao verificar duplicatas do extrato. A importação segue sem o aviso.");
            return 0;
        }
    }

    private async Task<CategoryResolutionContext> BuildContextAsync(Guid userId)
    {
        var categories = await _categoryRepository.GetAllByUserIdAsync(userId);
        var rules = await _categoryRuleRepository.GetAllByUserIdAsync(userId);
        var history = await _transactionRepository.GetDescriptionCategoryHistoryAsync(userId);

        return CategoryResolutionContext.Build(categories, rules, history);
    }

    /// <summary>
    /// Pede à IA uma sugestão só para o que sobrou sem categoria, olhando todos os
    /// arquivos do lote de uma vez. Falha aqui é registrada e ignorada: as linhas
    /// continuam em branco para o usuário marcar.
    /// </summary>
    private async Task EnrichWithAiAsync(
        StatementImportResultDto result, CategoryResolutionContext context, Lazy<Task<bool>> aiAvailability)
    {
        var unresolved = CategoryResolver.Unresolved(result.Transactions);
        if (unresolved.Count == 0 || context.CategoryIdsByName.Count == 0)
            return;

        if (result.AiUnavailable || !await aiAvailability.Value)
        {
            result.AiUnavailable = true;
            result.Warnings.Add(
                "O agente de IA está indisponível: as transações sem categoria precisam ser classificadas manualmente.");
            return;
        }

        var descriptions = unresolved.Select(t => t.Description).Distinct().ToList();
        var suggestions = await _aiIntegrationService.SuggestCategoriesAsync(
            descriptions, context.CategoryIdsByName.Keys.ToList());

        if (suggestions is null)
        {
            result.AiUnavailable = true;
            result.Warnings.Add(
                "Não foi possível obter sugestões de categoria da IA: classifique manualmente as linhas em branco.");
            return;
        }

        var applied = 0;
        foreach (var transaction in unresolved)
        {
            if (!suggestions.TryGetValue(transaction.Description, out var categoryName)
                || string.IsNullOrWhiteSpace(categoryName))
                continue;

            if (context.CategoryIdsByName.TryGetValue(categoryName.Trim(), out var categoryId))
            {
                // A IA escolheu entre as categorias que o usuário já tem, mas ela
                // acerta menos que uma regra confirmada: fica como sugestão.
                transaction.SuggestedCategoryName = context.CategoryNamesById[categoryId];
            }
            else
            {
                transaction.SuggestedCategoryName = StatementTextNormalizer.ToTitleCase(categoryName);
            }

            transaction.IsSuggestion = true;
            applied++;
        }

        result.AiUsed = result.AiUsed || applied > 0;
    }

    private static FileParseOutcome Unreadable(StatementFile file, bool aiUnavailable)
    {
        var message = aiUnavailable
            ? $"Não foi possível ler '{file.FileName}': o formato não é reconhecido automaticamente e o agente de IA está indisponível."
            : $"Não foi possível ler '{file.FileName}': nenhuma transação foi encontrada no arquivo.";

        return new FileParseOutcome(file, Array.Empty<ParsedStatementEntry>(), null, false, aiUnavailable, message);
    }

    private static FileImportSummaryDto BuildSummary(FileParseOutcome outcome)
    {
        var success = outcome.Entries.Count > 0;
        return new FileImportSummaryDto
        {
            FileName = outcome.File.FileName,
            Success = success,
            Message = success ? null : outcome.ErrorMessage,
            ParserUsed = success ? outcome.ParserUsed : null,
            AiUsed = outcome.AiUsed,
            TransactionCount = outcome.Entries.Count
        };
    }

    private static string BuildFileWarning(FileParseOutcome outcome) =>
        $"{outcome.File.FileName}: {outcome.ErrorMessage}";

    private static void ApplyAllFailed(StatementImportResultDto result, IReadOnlyList<FileParseOutcome> outcomes)
    {
        result.Success = false;
        result.ParserUsed = null;
        result.Files = outcomes.Select(BuildSummary).ToList();
        result.AiUnavailable = outcomes.Any(o => o.AiUnavailable);

        // Com um arquivo só, a mensagem fica idêntica à de antes desta função
        // existir — é o que preserva o texto que os testes de arquivo único conferem.
        result.Message = outcomes.Count == 1
            ? outcomes[0].ErrorMessage
            : $"Não foi possível ler nenhum dos {outcomes.Count} arquivos enviados.";

        if (outcomes.Count > 1)
        {
            foreach (var outcome in outcomes)
                result.Warnings.Add(BuildFileWarning(outcome));
        }
    }
}
