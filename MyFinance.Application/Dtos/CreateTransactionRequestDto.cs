using System.ComponentModel.DataAnnotations;
using MyFinance.Domain.Enums;
using System;

namespace MyFinance.Application.Dtos;

public class CreateTransactionRequestDto
{
    [Required(ErrorMessage = "A Descri��o � obrigat�ria.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "A Descri��o deve ter entre 2 e 150 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Valor � obrigat�rio.")]
    [DataType(DataType.Currency)]
    [Range(0.01, 10000000.00, ErrorMessage = "O Valor deve ser maior que zero.")] // Valor n�o pode ser zero ou negativo
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "O Tipo da transa��o � obrigat�rio (Income ou Expense).")]
    [EnumDataType(typeof(TransactionType), ErrorMessage = "O Tipo de transa��o � inv�lido.")]
    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "A Data da transa��o � obrigat�ria.")]
    [DataType(DataType.Date)] // Apenas a data � relevante para o usu�rio informar
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "A Conta � obrigat�ria.")]
    public Guid AccountId { get; set; }

    [Required(ErrorMessage = "A Categoria � obrigat�ria.")]
    public Guid CategoryId { get; set; }

    public Guid? FinancialGoalId { get; set; }
}