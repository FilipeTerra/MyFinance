using System.ComponentModel.DataAnnotations;

namespace MyFinance.Application.Dtos;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "O Nome � obrigat�rio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O Nome deve ter entre 3 e 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Email � obrigat�rio.")]
    [EmailAddress(ErrorMessage = "O formato do Email � inv�lido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Senha � obrigat�ria.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A Senha deve ter pelo menos 6 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "As senhas n�o coincidem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}