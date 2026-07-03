namespace MyFinance.Application.Dtos;

public class UserProfileResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal? MonthlyIncome { get; set; }
}
