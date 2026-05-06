namespace MyFinance.Domain.Enums;

/// <summary>
/// Define os tipos de contas financeiras que o usu�rio pode cadastrar.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Contas banc�rias tradicionais.
    /// </summary>
    ContaCorrente = 1,

    /// <summary>
    /// Contas para guardar dinheiro.
    /// </summary>
    Poupanca = 2,

    /// <summary>
    /// Dinheiro f�sico (em esp�cie).
    /// </summary>
    Carteira = 3,

    /// <summary>
    /// Cart�es de cr�dito, focados em limite e fatura.
    /// </summary>
    CartaoCredito = 4,

    /// <summary>
    /// Contas de corretoras, fundos, etc.
    /// </summary>
    Investimento = 5
}