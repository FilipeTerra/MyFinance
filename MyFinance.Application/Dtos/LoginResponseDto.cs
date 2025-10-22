namespace MyFinance.Application.Dtos;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}