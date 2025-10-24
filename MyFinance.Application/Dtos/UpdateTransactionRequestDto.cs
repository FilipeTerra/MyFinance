using System.ComponentModel.DataAnnotations;
using MyFinance.Domain.Enums;
using System;

namespace MyFinance.Application.Dtos;

public class UpdateTransactionRequestDto
{
	// As mesmas validações do Create DTO
	[Required(ErrorMessage = "A Descrição é obrigatória.")]
	[StringLength(150, MinimumLength = 2, ErrorMessage = "A Descrição deve ter entre 2 e 150 caracteres.")]
	public string Description { get; set; } = string.Empty;

	[Required(ErrorMessage = "O Valor é obrigatório.")]
	[DataType(DataType.Currency)]
	[Range(0.01, 10000000.00, ErrorMessage = "O Valor deve ser maior que zero.")]
	public decimal Amount { get; set; }

	[Required(ErrorMessage = "O Tipo da transação é obrigatório (Income ou Expense).")]
	[EnumDataType(typeof(TransactionType), ErrorMessage = "O Tipo de transação é inválido.")]
	public TransactionType Type { get; set; }

	[Required(ErrorMessage = "A Data da transação é obrigatória.")]
	[DataType(DataType.Date)]
	public DateTime Date { get; set; }

	[Required(ErrorMessage = "A Conta é obrigatória.")]
	public Guid AccountId { get; set; }

	[Required(ErrorMessage = "A Categoria é obrigatória.")]
	public Guid CategoryId { get; set; }
}