using System;

namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Representa um lançamento individual de despesa em destaque (ex.: maiores gastos do período).
/// </summary>
public class TopExpenseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Valor da despesa, sempre positivo.
    /// </summary>
    public decimal Amount { get; set; }

    public DateTime Date { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}
