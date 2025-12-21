using System.ComponentModel.DataAnnotations;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos;

public class AccountRequestDto
{
    [Required(ErrorMessage = "O Nome da conta é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O Nome deve ter entre 3 e 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Tipo da conta é obrigatório.")]
    [EnumDataType(typeof(AccountType), ErrorMessage = "O Tipo de conta é inválido.")]
    public AccountType Type { get; set; }

    [Required(ErrorMessage = "O Saldo Inicial é obrigatório.")]
    [DataType(DataType.Currency)]
    [Range(-10000000, 10000000, ErrorMessage = "Valor de saldo inválido (entre -10 milhões e 10 milhões).")]
    public decimal InitialBalance { get; set; }
}