using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Entities;

public class Account
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }

    /// <summary>
    /// O saldo inicial que o usu�rio informou ao criar a conta.
    /// </summary>
    public decimal InitialBalance { get; set; }

    public DateTime CreatedAt { get; set; }

    // --- Relacionamento com User ---

    /// <summary>
    /// Chave Estrangeira (FK) para a tabela Users.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Propriedade de Navega��o para o EF Core entender o relacionamento.
    /// Uma conta pertence a UM usu�rio.
    /// </summary>
    public User User { get; set; } = null!;
}