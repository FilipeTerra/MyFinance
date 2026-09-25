namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Uma parcela já lançada, como está gravada. É a matéria-prima crua do "comprometido":
/// o agrupamento por compra e a projeção dos meses seguintes são feitos fora do banco,
/// porque dependem da normalização da descrição.
/// </summary>
public class InstallmentRowDto
{
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public Guid AccountId { get; set; }

    public int InstallmentNumber { get; set; }

    public int InstallmentTotal { get; set; }

    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}
