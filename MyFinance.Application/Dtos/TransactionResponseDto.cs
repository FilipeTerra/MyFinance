using MyFinance.Domain.Enums;
using System;

namespace MyFinance.Application.Dtos;

public class TransactionResponseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string TypeName { get; set; } = string.Empty; // "Income" ou "Expense"
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty; // Nome da conta associada

    // --- Futuro ---
    // public Guid? CategoryId { get; set; }
    // public string? CategoryName { get; set; }
}