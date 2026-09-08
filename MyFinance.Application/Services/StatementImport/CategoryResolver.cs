using MyFinance.Application.Dtos;
using MyFinance.Application.Dtos.StatementImport;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Atribui categoria às linhas do extrato sem consultar serviço nenhum, na ordem
/// do mais confiável para o menos: regra que o usuário confirmou, histórico dele,
/// e por fim o que o próprio documento informa. O que não se resolve fica em
/// branco de propósito — chutar categoria errada custa mais ao usuário do que
/// deixá-lo escolher na tela de revisão.
/// </summary>
public static class CategoryResolver
{
    public static List<AiTransactionResponseDto> Resolve(
        IReadOnlyList<ParsedStatementEntry> entries,
        Guid accountId,
        CategoryResolutionContext context)
    {
        var results = new List<AiTransactionResponseDto>(entries.Count);

        foreach (var entry in entries)
        {
            var dto = new AiTransactionResponseDto
            {
                Date = entry.Date,
                Description = entry.Description,
                Amount = entry.Amount,
                AccountId = accountId
            };

            Apply(dto, entry, context);
            results.Add(dto);
        }

        return results;
    }

    private static void Apply(
        AiTransactionResponseDto dto, ParsedStatementEntry entry, CategoryResolutionContext context)
    {
        var key = StatementTextNormalizer.Normalize(entry.Description);

        // 1. Regra aprendida: o usuário já disse explicitamente onde isso entra.
        if (key.Length > 0 && context.RulesByDescriptionKey.TryGetValue(key, out var ruleCategoryId))
        {
            dto.CategoryId = ruleCategoryId;
            return;
        }

        // 2. Histórico com a mesma descrição: igualmente confiável, só não foi
        // confirmado por uma importação.
        if (key.Length > 0 && context.HistoryByDescriptionKey.TryGetValue(key, out var historyCategoryId))
        {
            dto.CategoryId = historyCategoryId;
            return;
        }

        // 3. Mesmo comerciante em outra praça ou com outro código: só sugere,
        // porque "CARREFOUR" pode ser tanto supermercado quanto posto.
        var merchant = StatementTextNormalizer.MerchantToken(entry.Description);
        if (merchant.Length > 0 && context.HistoryByMerchantToken.TryGetValue(merchant, out var merchantCategoryId))
        {
            dto.SuggestedCategoryName = context.CategoryNamesById[merchantCategoryId];
            dto.IsSuggestion = true;
            return;
        }

        // 4. Categoria que veio no próprio arquivo (o CSV do Inter traz uma).
        if (!string.IsNullOrWhiteSpace(entry.FileCategoryName))
        {
            if (context.CategoryIdsByName.TryGetValue(entry.FileCategoryName.Trim(), out var fileCategoryId))
            {
                dto.CategoryId = fileCategoryId;
                return;
            }

            dto.SuggestedCategoryName = entry.FileCategoryName;
            dto.IsSuggestion = true;
            return;
        }

        // 5. Sem pista alguma: o usuário classifica na revisão.
        dto.CategoryId = null;
        dto.SuggestedCategoryName = null;
        dto.IsSuggestion = false;
    }

    /// <summary>Linhas que continuam sem categoria e sem sugestão após a cadeia determinística.</summary>
    public static List<AiTransactionResponseDto> Unresolved(IEnumerable<AiTransactionResponseDto> transactions) =>
        transactions
            .Where(t => t.CategoryId is null && string.IsNullOrWhiteSpace(t.SuggestedCategoryName))
            .ToList();
}
