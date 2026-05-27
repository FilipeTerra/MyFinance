namespace MyFinance.Domain.Enums;

/// <summary>
/// Define os tipos de transa��es financeiras (Entrada ou Sa�da).
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Representa uma entrada de dinheiro (ex: Sal�rio, Venda).
    /// </summary>
    Income = 1, // Receita

    /// <summary>
    /// Representa uma sa�da de dinheiro (ex: Almo�o, Compra).
    /// </summary>
    Expense = 2, // Despesa

    /// <summary>
    /// Representa um aporte numa Meta Financeira.
    /// </summary>
    Investment = 3 // Investimento
}