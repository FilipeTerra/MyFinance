using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Projeção intermediária (repositório → serviço) de despesas agrupadas por mês e categoria.
/// Não é exposta diretamente pela API; o serviço a organiza em <see cref="MonthlyPointDto"/>.
/// </summary>
public class MonthlyCategoryTotalDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int TransactionCount { get; set; }

    /// <summary>Classificação de cortabilidade dada pelo usuário à categoria.</summary>
    public ExpenseNature Nature { get; set; }
}
