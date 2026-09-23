using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Total de despesas agrupado por categoria em um período.
/// </summary>
public class CategoryExpenseDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Soma das despesas da categoria no período, sempre positiva.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Participação percentual da categoria sobre o total de despesas do período (0 a 100).
    /// </summary>
    public decimal Percentage { get; set; }

    public int TransactionCount { get; set; }

    /// <summary>
    /// Classificação de cortabilidade dada pelo usuário à categoria. Usada pela sugestão
    /// de aporte para decidir o que pode entrar em um plano de corte.
    /// </summary>
    public ExpenseNature Nature { get; set; }
}
