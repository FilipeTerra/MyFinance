using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Tudo o que o usuário já nos ensinou sobre suas categorias, carregado uma única
/// vez por importação e indexado pela chave normalizada da descrição.
/// </summary>
public sealed class CategoryResolutionContext
{
    private CategoryResolutionContext(
        IReadOnlyDictionary<string, Guid> rulesByDescriptionKey,
        IReadOnlyDictionary<string, Guid> historyByDescriptionKey,
        IReadOnlyDictionary<string, Guid> historyByMerchantToken,
        IReadOnlyDictionary<string, Guid> categoryIdsByName,
        IReadOnlyDictionary<Guid, string> categoryNamesById)
    {
        RulesByDescriptionKey = rulesByDescriptionKey;
        HistoryByDescriptionKey = historyByDescriptionKey;
        HistoryByMerchantToken = historyByMerchantToken;
        CategoryIdsByName = categoryIdsByName;
        CategoryNamesById = categoryNamesById;
    }

    /// <summary>Regras confirmadas pelo usuário — a fonte mais confiável.</summary>
    public IReadOnlyDictionary<string, Guid> RulesByDescriptionKey { get; }

    /// <summary>Categoria mais usada para cada descrição já lançada.</summary>
    public IReadOnlyDictionary<string, Guid> HistoryByDescriptionKey { get; }

    /// <summary>Categoria mais usada para cada comerciante, ignorando praça e código.</summary>
    public IReadOnlyDictionary<string, Guid> HistoryByMerchantToken { get; }

    public IReadOnlyDictionary<string, Guid> CategoryIdsByName { get; }

    public IReadOnlyDictionary<Guid, string> CategoryNamesById { get; }

    public static CategoryResolutionContext Build(
        IEnumerable<Category> categories,
        IEnumerable<CategoryRule> rules,
        IEnumerable<DescriptionCategoryCount> history)
    {
        var categoryList = categories.ToList();
        var namesById = categoryList
            .GroupBy(c => c.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var idsByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in categoryList)
            idsByName.TryAdd(category.Name.Trim(), category.Id);

        // Regra apontando para categoria já excluída não serve para nada — e
        // aplicá-la geraria transação com categoria inexistente.
        var ruleMap = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var rule in rules.Where(r => namesById.ContainsKey(r.CategoryId)))
            ruleMap[rule.DescriptionKey] = rule.CategoryId;

        var historyList = history.Where(h => namesById.ContainsKey(h.CategoryId)).ToList();

        return new CategoryResolutionContext(
            ruleMap,
            MostFrequentBy(historyList, h => StatementTextNormalizer.Normalize(h.Description)),
            MostFrequentBy(historyList, h => StatementTextNormalizer.MerchantToken(h.Description)),
            idsByName,
            namesById);
    }

    /// <summary>
    /// Agrupa o histórico pela chave escolhida e fica com a categoria mais usada.
    /// Empate é desfeito pelo Guid, só para o resultado não variar entre chamadas.
    /// </summary>
    private static Dictionary<string, Guid> MostFrequentBy(
        IReadOnlyList<DescriptionCategoryCount> history,
        Func<DescriptionCategoryCount, string> keySelector)
    {
        return history
            .Select(h => (Key: keySelector(h), h.CategoryId, h.Count))
            .Where(x => x.Key.Length > 0)
            .GroupBy(x => x.Key, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(x => x.CategoryId)
                    .OrderByDescending(byCategory => byCategory.Sum(x => x.Count))
                    .ThenBy(byCategory => byCategory.Key)
                    .First()
                    .Key,
                StringComparer.Ordinal);
    }
}
