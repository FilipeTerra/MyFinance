using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }

    /// <summary>
    /// O saldo inicial que o usuário informou ao criar a conta.
    /// </summary>
    public decimal InitialBalance { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Conta pertence a UM usuário.
    /// </summary>
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Account(string name, AccountType type, decimal initialBalance, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome da conta é obrigatório.", nameof(name));

        if (userId == Guid.Empty)
            throw new ArgumentException("A conta precisa pertencer a um usuário.", nameof(userId));

        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        InitialBalance = initialBalance;
        Balance = initialBalance;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateBalance(decimal amount)
    {
        Balance += amount;
    }

    /// <summary>
    /// Atualiza os dados cadastrais da conta (nome e tipo). O saldo é alterado
    /// apenas via <see cref="UpdateBalance"/>, nunca diretamente.
    /// </summary>
    public void Rename(string name, AccountType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome da conta é obrigatório.", nameof(name));

        Name = name;
        Type = type;
    }
}
