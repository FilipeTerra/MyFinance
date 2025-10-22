using System.ComponentModel.DataAnnotations; 

namespace MyFinance.Application.Dtos;

public class LoginRequestDto
{
    [Required(ErrorMessage = "O Email � obrigat�rio.")]
    [EmailAddress(ErrorMessage = "O formato do Email inv�lido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Senha � obrigat�ria.")]
    public string Password { get; set; } = string.Empty;
}