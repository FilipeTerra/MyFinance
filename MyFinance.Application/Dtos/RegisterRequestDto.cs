using System.ComponentModel.DataAnnotations;

namespace MyFinance.Application.Dtos;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "O Nome é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O Nome deve ter entre 3 e 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do Email é inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Senha é obrigatória.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A Senha deve ter pelo menos 6 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "As senhas não coincidem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}