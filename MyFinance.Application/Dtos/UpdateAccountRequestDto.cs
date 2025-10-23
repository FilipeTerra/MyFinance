using System.ComponentModel.DataAnnotations;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos;

public class UpdateAccountRequestDto
{
    [Required(ErrorMessage = "O Nome da conta � obrigat�rio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O Nome deve ter entre 3 e 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Tipo da conta � obrigat�rio.")]
    [EnumDataType(typeof(AccountType), ErrorMessage = "O Tipo de conta � inv�lido.")]
    public AccountType Type { get; set; }
}