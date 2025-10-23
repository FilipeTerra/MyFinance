using MyFinance.Domain.Enums; // Importa o Enum TransactionType
using System;

namespace MyFinance.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; } // Valor da transa��o (sempre positivo)
    public TransactionType Type { get; set; } // Income ou Expense

    /// <summary>
    /// Data em que a transa��o ocorreu (informada pelo usu�rio).
    /// </summary>
    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; } // Data de registro no sistema

    // --- Relacionamento com Account ---

    /// <summary>
    /// Chave Estrangeira (FK) para a tabela Accounts.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Propriedade de Navega��o para o EF Core.
    /// Uma transa��o pertence a UMA conta.
    /// </summary>
    public Account Account { get; set; } = null!;

    // --- Relacionamento (Futuro) com Category ---
    // public Guid? CategoryId { get; set; } // Pode ser nulo inicialmente
    // public Category? Category { get; set; } 
}