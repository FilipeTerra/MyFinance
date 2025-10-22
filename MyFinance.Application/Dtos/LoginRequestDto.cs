using System.ComponentModel.DataAnnotations; 

namespace MyFinance.Application.Dtos;

public class LoginRequestDto
{
    [Required(ErrorMessage = "O Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do Email inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Senha é obrigatória.")]
    public string Password { get; set; } = string.Empty;
}