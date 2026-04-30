using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } // Income ou Expense

    /// <summary>
    /// Data em que a transação ocorreu (informada pelo usuário).
    /// </summary>
    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

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
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Propriedade de Navegação para o EF Core.
    /// Uma transação pertence a UMA categoria.
    /// </summary>
    public Category Category { get; set; } = null!;

    public Transaction(string description, decimal amount, TransactionType type, DateTime date, Guid accountId, Guid categoryId)
    {
        Id = Guid.NewGuid(); // Gerar um novo Id para cada transação
        Description = description;
        Amount = amount;
        Type = type;
        Date = date;
        AccountId = accountId;
        CategoryId = categoryId;
    }
}