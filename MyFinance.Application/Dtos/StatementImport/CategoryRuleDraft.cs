namespace MyFinance.Application.Dtos.StatementImport;

/// <summary>Regra descrição → categoria a ser gravada, já com a chave normalizada.</summary>
public sealed record CategoryRuleDraft(string DescriptionKey, Guid CategoryId);

/// <summary>
/// Quantas vezes uma descrição já foi lançada em determinada categoria pelo
/// usuário. É a matéria-prima da categorização por histórico.
/// </summary>
public sealed record DescriptionCategoryCount(string Description, Guid CategoryId, int Count);
