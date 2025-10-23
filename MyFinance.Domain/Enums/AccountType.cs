namespace MyFinance.Domain.Enums;

/// <summary>
/// Define os tipos de contas financeiras que o usuário pode cadastrar.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Contas bancárias tradicionais.
    /// </summary>
    ContaCorrente = 1,

    /// <summary>
    /// Contas para guardar dinheiro.
    /// </summary>
    Poupanca = 2,

    /// <summary>
    /// Dinheiro físico (em espécie).
    /// </summary>
    Carteira = 3,

    /// <summary>
    /// Cartões de crédito, focados em limite e fatura.
    /// </summary>
    CartaoCredito = 4,

    /// <summary>
    /// Contas de corretoras, fundos, etc.
    /// </summary>
    Investimento = 5
}