namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Projeção intermediária (repositório → serviço) de receitas e despesas totais por mês.
/// Não é exposta diretamente pela API; o serviço a organiza em <see cref="MonthlyPointDto"/>.
/// </summary>
public class MonthlyFlowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalIncome { get; set; }
}
