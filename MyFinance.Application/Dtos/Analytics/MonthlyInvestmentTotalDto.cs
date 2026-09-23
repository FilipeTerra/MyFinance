namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Projeção intermediária (repositório → serviço) do total aportado por mês.
/// Aporte não é despesa nem receita, então não cabe em <see cref="MonthlyFlowDto"/>.
/// </summary>
public class MonthlyInvestmentTotalDto
{
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>Soma dos aportes do mês, sempre positiva.</summary>
    public decimal Total { get; set; }
}
