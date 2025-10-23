namespace MyFinance.Domain.Enums;

/// <summary>
/// Define os tipos de transações financeiras (Entrada ou Saída).
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Representa uma entrada de dinheiro (ex: Salário, Venda).
    /// </summary>
    Income = 1, // Receita

    /// <summary>
    /// Representa uma saída de dinheiro (ex: Almoço, Compra).
    /// </summary>
    Expense = 2 // Despesa
}