namespace MyFinance.Application.Dtos.StatementImport;

/// <summary>
/// Uma linha de extrato já extraída do arquivo, antes da resolução de categoria.
/// </summary>
/// <param name="Date">Data em que a transação ocorreu, conforme o documento.</param>
/// <param name="Description">Descrição bruta do lançamento, como aparece no arquivo.</param>
/// <param name="Amount">
/// Valor com o sinal já aplicado: despesa negativa, receita positiva —
/// mesma convenção usada no restante da aplicação.
/// </param>
/// <param name="FileCategoryName">
/// Categoria informada pelo próprio documento (o extrato do Inter traz uma
/// coluna "Categoria"), quando existir e não for genérica.
/// </param>
public sealed record ParsedStatementEntry(
    DateTime Date,
    string Description,
    decimal Amount,
    string? FileCategoryName = null);
