using System;
using System.Collections.Generic;

namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Visão geral dos gastos do usuário em um período, com comparação em relação ao período anterior de mesma duração.
/// </summary>
public class ExpenseOverviewResponseDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal TotalExpenses { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }

    /// <summary>
    /// Média mensal de despesas, considerando a quantidade de meses cobertos pelo período (mínimo 1).
    /// </summary>
    public decimal MonthlyAverage { get; set; }

    /// <summary>
    /// Total de despesas no período anterior de mesma duração, usado como base de comparação.
    /// </summary>
    public decimal PreviousTotalExpenses { get; set; }

    /// <summary>
    /// Variação em reais em relação ao período anterior (TotalExpenses - PreviousTotalExpenses).
    /// </summary>
    public decimal VariationAmount { get; set; }

    /// <summary>
    /// Variação percentual em relação ao período anterior. Nulo quando o período anterior não teve despesas
    /// (evita divisão por zero / valores infinitos no front-end).
    /// </summary>
    public decimal? VariationPercent { get; set; }

    /// <summary>
    /// Despesas do período atual agrupadas por categoria, ordenadas da maior para a menor.
    /// </summary>
    public List<CategoryExpenseDto> Categories { get; set; } = new();

    /// <summary>
    /// Despesas do período anterior agrupadas por categoria, na mesma ordem de <see cref="Categories"/>
    /// sempre que possível, para facilitar a comparação lado a lado no front-end.
    /// </summary>
    public List<CategoryExpenseDto> PreviousCategories { get; set; } = new();

    /// <summary>
    /// Os maiores lançamentos individuais de despesa no período.
    /// </summary>
    public List<TopExpenseDto> TopExpenses { get; set; } = new();
}
