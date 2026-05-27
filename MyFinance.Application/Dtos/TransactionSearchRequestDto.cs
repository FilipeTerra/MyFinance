using System;
using System.ComponentModel.DataAnnotations;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos;

/// <summary>
/// DTO para encapsular os par�metros da busca de transa��es.
/// </summary>
public class TransactionSearchRequestDto
{
	[Required(ErrorMessage = "A Conta � obrigat�ria.")]
	public Guid AccountId { get; set; }

	public string? SearchText { get; set; }

	[DataType(DataType.Date)]
	public DateTime? StartDate { get; set; }

	[DataType(DataType.Date)]
	public DateTime? EndDate { get; set; }

	[DataType(DataType.Currency)]
	[Range(0.01, 10000000.00, ErrorMessage = "O Valor deve ser maior que zero.")]
	public decimal? Amount { get; set; }

	public TransactionType? Type { get; set; }

	public Guid? CategoryId { get; set; }

	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 20;
}