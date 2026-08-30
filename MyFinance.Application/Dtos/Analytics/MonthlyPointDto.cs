using System.Collections.Generic;

namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Ponto mensal da linha do tempo de gastos, com o detalhamento por categoria daquele mês.
/// </summary>
public class MonthlyPointDto
{
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>
    /// Rótulo no formato "yyyy-MM" (ex.: "2026-08"), conveniente para ordenação e chave de eixo X no front-end.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    public decimal TotalExpenses { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal Balance { get; set; }

    public List<CategoryExpenseDto> Categories { get; set; } = new();
}
