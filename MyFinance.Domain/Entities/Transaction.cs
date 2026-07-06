using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; } // Income ou Expense

    /// <summary>
    /// Data em que a transação ocorreu (informada pelo usuário).
    /// </summary>
    public DateTime Date { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.Now;

    // --- Relacionamento com Account ---

    /// <summary>
    /// Chave Estrangeira (FK) para a tabela Accounts.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Conta à qual esta transação pertence.
    /// </summary>
    public Account Account { get; private set; } = null!;

    // --- Relacionamento com Category ---
    /// <summary>
    /// Chave Estrangeira (FK) para a tabela Categories.
    /// </summary>
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Categoria à qual esta transação pertence.
    /// </summary>
    public Category Category { get; private set; } = null!;

    // --- Relacionamento com FinancialGoal (opcional) ---
    /// <summary>
    /// Chave Estrangeira (FK) opcional para a tabela FinancialGoals.
    /// Preenchida apenas quando o tipo for Investment (aporte em meta).
    /// </summary>
    public Guid? FinancialGoalId { get; private set; }

    public FinancialGoal? FinancialGoal { get; private set; }

    // --- Relacionamento com Investimento (opcional) ---
    /// <summary>
    /// Chave Estrangeira (FK) opcional para a tabela Investimentos.
    /// Preenchida quando a transação é a origem (aporte) de um investimento —
    /// é o que dá ao investimento uma origem concreta (conta bancária debitada).
    /// </summary>
    public Guid? InvestimentoId { get; private set; }

    public Investimento? Investimento { get; private set; }

    public Transaction(string description, decimal amount, TransactionType type, DateTime date, Guid accountId, Guid categoryId, Guid? financialGoalId = null, Guid? investimentoId = null)
    {
        Validate(description, accountId, categoryId);

        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        Type = type;
        Date = date;
        AccountId = accountId;
        CategoryId = categoryId;
        FinancialGoalId = financialGoalId;
        InvestimentoId = investimentoId;
    }

    /// <summary>
    /// Reatribui os dados editáveis de uma transação existente (usado em edições feitas pelo usuário).
    /// </summary>
    public void Reassign(string description, decimal amount, TransactionType type, DateTime date, Guid accountId, Guid categoryId)
    {
        Validate(description, accountId, categoryId);

        Description = description;
        Amount = amount;
        Type = type;
        Date = date;
        AccountId = accountId;
        CategoryId = categoryId;
    }

    private static void Validate(string description, Guid accountId, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descrição da transação é obrigatória.", nameof(description));

        if (accountId == Guid.Empty)
            throw new ArgumentException("A transação precisa pertencer a uma conta.", nameof(accountId));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("A transação precisa pertencer a uma categoria.", nameof(categoryId));
    }
}
