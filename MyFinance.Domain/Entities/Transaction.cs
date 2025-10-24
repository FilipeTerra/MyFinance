using MyFinance.Domain.Enums;
using MyFinance.Domain.Entities;
using System;

namespace MyFinance.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; } // Valor da transação (sempre positivo)
    public TransactionType Type { get; set; } // Income ou Expense

    /// <summary>
    /// Data em que a transação ocorreu (informada pelo usuário).
    /// </summary>
    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; } // Data de registro no sistema

    // --- Relacionamento com Account ---

    /// <summary>
    /// Chave Estrangeira (FK) para a tabela Accounts.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Propriedade de Navegação para o EF Core. Uma transação pertence a UMA conta.
    /// </summary>
    public Account Account { get; set; } = null!;

    // --- Relacionamento com Category ---
    /// <summary>
    /// Chave Estrangeira (FK) para a tabela Categories.
    /// </summary>
    public Guid CategoryId { get; set; } // Agora é obrigatória

    /// <summary>
    /// Propriedade de Navegação para o EF Core.
    /// Uma transação pertence a UMA categoria.
    /// </summary>
    public Category Category { get; set; } = null!;
}