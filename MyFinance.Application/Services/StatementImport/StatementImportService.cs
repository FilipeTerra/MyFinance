using Microsoft.Extensions.Logging;
using MyFinance.Application.Dtos;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Orquestra a importação de extrato. A ordem é deliberada: primeiro o que
/// funciona sempre (parsers determinísticos e o que o usuário já ensinou) e só
/// depois o agente de IA, que é tratado como opcional em todas as etapas —
/// se ele estiver fora, a importação acontece do mesmo jeito.
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

    public async Task<StatementImportResultDto> ImportAsync(StatementFile file, Guid accountId, Guid userId)
    {
        var result = new StatementImportResultDto();

        var entries = ParseDeterministically(file, out var parserName);
        result.ParserUsed = parserName;

        if (entries.Count == 0)
        {
            entries = await ExtractWithAiAsync(file, result);

            if (entries.Count == 0)
                return Failed(result, file);
        }

        var context = await BuildContextAsync(userId);
        result.Transactions = CategoryResolver.Resolve(entries, accountId, context);

        result.DuplicateCount = await MarkDuplicatesAsync(result.Transactions, entries, accountId, userId);

        await EnrichWithAiAsync(result, context);

        _logger.LogInformation(
            "Extrato '{Arquivo}' importado por {Parser}: {Total} transação(ões), "
            + "{SemCategoria} sem categoria, {Duplicadas} já existente(s) na conta.",
            file.FileName, result.ParserUsed, result.Transactions.Count,
            CategoryResolver.Unresolved(result.Transactions).Count, result.DuplicateCount);

        return result;
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

    private async Task<List<ParsedStatementEntry>> ExtractWithAiAsync(
        StatementFile file, StatementImportResultDto result)
    {
        if (!await _aiIntegrationService.IsAvailableAsync())
        {
            result.AiUnavailable = true;
            return new List<ParsedStatementEntry>();
        }

        var aiEntries = await _aiIntegrationService.ExtractStatementAsync(file);
        if (aiEntries is null)
        {
            result.AiUnavailable = true;
            return new List<ParsedStatementEntry>();
        }

        result.AiUsed = true;
        result.ParserUsed = AiParserName;
        return aiEntries.ToList();
    }

    /// <summary>
    /// Confronta o que foi lido com o que já está salvo na conta, na faixa de datas
    /// do próprio arquivo. Uma falha aqui não pode impedir a importação: no pior
    /// caso o usuário revisa sem o aviso de duplicata, que é o comportamento que
    /// existia antes.
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
    /// Pede à IA uma sugestão só para o que sobrou sem categoria. Falha aqui é
    /// registrada e ignorada: as linhas continuam em branco para o usuário marcar.
    /// </summary>
    private async Task EnrichWithAiAsync(StatementImportResultDto result, CategoryResolutionContext context)
    {
        var unresolved = CategoryResolver.Unresolved(result.Transactions);
        if (unresolved.Count == 0 || context.CategoryIdsByName.Count == 0)
            return;

        if (result.AiUnavailable || !await _aiIntegrationService.IsAvailableAsync())
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

    private static StatementImportResultDto Failed(StatementImportResultDto result, StatementFile file)
    {
        result.Success = false;
        result.ParserUsed = null;
        result.Message = result.AiUnavailable
            ? $"Não foi possível ler '{file.FileName}': o formato não é reconhecido automaticamente e o agente de IA está indisponível."
            : $"Não foi possível ler '{file.FileName}': nenhuma transação foi encontrada no arquivo.";

        return result;
    }
}
