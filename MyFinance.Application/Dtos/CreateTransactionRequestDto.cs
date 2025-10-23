using System.ComponentModel.DataAnnotations;
using MyFinance.Domain.Enums;
using System;

namespace MyFinance.Application.Dtos;

public class CreateTransactionRequestDto
{
    [Required(ErrorMessage = "A Descrição é obrigatória.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "A Descrição deve ter entre 2 e 150 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Valor é obrigatório.")]
    [DataType(DataType.Currency)]
    [Range(0.01, 10000000.00, ErrorMessage = "O Valor deve ser maior que zero.")] // Valor não pode ser zero ou negativo
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "O Tipo da transação é obrigatório (Income ou Expense).")]
    [EnumDataType(typeof(TransactionType), ErrorMessage = "O Tipo de transação é inválido.")]
    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "A Data da transação é obrigatória.")]
    [DataType(DataType.Date)] // Apenas a data é relevante para o usuário informar
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "A Conta é obrigatória.")]
    public Guid AccountId { get; set; }

    // --- Futuro ---
    // public Guid? CategoryId { get; set; } // Opcional ao criar
}