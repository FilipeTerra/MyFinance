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

    // --- Parcelamento (opcional) ---

    /// <summary>
    /// Número desta parcela dentro da compra parcelada, 1-based. Preenchido apenas
    /// na importação de extrato, quando o documento informa a parcela.
    /// </summary>
    public int? InstallmentNumber { get; private set; }

    /// <summary>
    /// Total de parcelas da compra. Anda sempre junto com <see cref="InstallmentNumber"/>.
    /// </summary>
    public int? InstallmentTotal { get; private set; }

    /// <summary>
    /// Quantas parcelas ainda vão vencer depois desta. Zero quando a compra já foi quitada
    /// ou quando a transação não é parcelada.
    /// </summary>
    public int InstallmentsRemaining =>
        InstallmentNumber.HasValue && InstallmentTotal.HasValue
            ? InstallmentTotal.Value - InstallmentNumber.Value
            : 0;

    public Transaction(string description, decimal amount, TransactionType type, DateTime date, Guid accountId, Guid categoryId, Guid? financialGoalId = null, Guid? investimentoId = null, int? installmentNumber = null, int? installmentTotal = null)
    {
        Validate(description, accountId, categoryId);
        ValidateInstallment(installmentNumber, installmentTotal);

        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        Type = type;
        Date = date;
        AccountId = accountId;
        CategoryId = categoryId;
        FinancialGoalId = financialGoalId;
        InvestimentoId = investimentoId;
        InstallmentNumber = installmentNumber;
        InstallmentTotal = installmentTotal;
    }

    /// <summary>
    /// Reatribui os dados editáveis de uma transação existente (usado em edições feitas pelo usuário).
    /// O parcelamento fica de fora de propósito: ele descreve o que o extrato informou sobre a compra,
    /// não um dado que o usuário edita ao corrigir descrição ou valor.
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

    /// <summary>
    /// O parcelamento chega do cliente (tela de revisão do extrato), então é validado aqui —
    /// é a última fronteira antes do banco. Um parser que leu errado não pode gravar
    /// "parcela 7 de 3" e envenenar a projeção de comprometido.
    /// </summary>
    private static void ValidateInstallment(int? installmentNumber, int? installmentTotal)
    {
        if (installmentNumber is null && installmentTotal is null)
            return;

        if (installmentNumber is null || installmentTotal is null)
            throw new ArgumentException(
                "O número e o total de parcelas precisam ser informados juntos.", nameof(installmentNumber));

        if (installmentTotal < 1 || installmentTotal > MaxInstallments)
            throw new ArgumentException(
                $"O total de parcelas precisa estar entre 1 e {MaxInstallments}.", nameof(installmentTotal));

        if (installmentNumber < 1 || installmentNumber > installmentTotal)
            throw new ArgumentException(
                "O número da parcela precisa estar entre 1 e o total de parcelas.", nameof(installmentNumber));
    }

    /// <summary>Teto de sanidade para parcelamento lido de extrato.</summary>
    public const int MaxInstallments = 99;
}
