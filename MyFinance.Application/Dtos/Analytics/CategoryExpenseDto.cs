using System;

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
}
