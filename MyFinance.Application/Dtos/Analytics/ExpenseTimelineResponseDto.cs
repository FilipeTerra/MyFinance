using System.Collections.Generic;

namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Evolução mensal dos gastos do usuário, em ordem cronológica crescente e sem lacunas
/// (meses sem lançamentos aparecem com os totais zerados).
/// </summary>
public class ExpenseTimelineResponseDto
{
    public List<MonthlyPointDto> Months { get; set; } = new();
}
