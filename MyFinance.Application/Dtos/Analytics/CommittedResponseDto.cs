namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// O que o usuário já deve dos próximos meses por compras parceladas no cartão.
/// Não depende de período: é sempre "a partir de hoje".
/// </summary>
public class CommittedResponseDto
{
    /// <summary>Soma de todas as parcelas que ainda vão vencer.</summary>
    public decimal TotalCommitted { get; set; }

    /// <summary>Quanto de parcela vence no próximo mês — o patamar atual do compromisso.</summary>
    public decimal NextMonthAmount { get; set; }

    /// <summary>Quantidade de compras parceladas ainda em aberto.</summary>
    public int PurchaseCount { get; set; }

    /// <summary>Mês em que a última parcela em aberto vence, no formato "aaaa-MM".</summary>
    public string? LastDueMonth { get; set; }

    public List<CommittedPurchaseDto> Purchases { get; set; } = new();

    public List<CommittedMonthDto> Schedule { get; set; } = new();
}

/// <summary>Uma compra parcelada em aberto.</summary>
public class CommittedPurchaseDto
{
    /// <summary>Descrição como veio no extrato, sem o sufixo de parcela.</summary>
    public string Description { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal InstallmentAmount { get; set; }

    public int CurrentInstallment { get; set; }

    public int TotalInstallments { get; set; }

    public int RemainingInstallments { get; set; }

    /// <summary>Quanto ainda falta pagar desta compra.</summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>Mês da última parcela, no formato "aaaa-MM".</summary>
    public string LastDueMonth { get; set; } = string.Empty;
}

/// <summary>Quanto de parcela vence num mês futuro.</summary>
public class CommittedMonthDto
{
    /// <summary>Mês no formato "aaaa-MM", como o resto da análise de gastos.</summary>
    public string Month { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int PurchaseCount { get; set; }

    /// <summary>
    /// Quanto do orçamento mensal já terá sido liberado até este mês, comparado ao
    /// que vence no próximo mês.
    /// </summary>
    public decimal ReleasedAmount { get; set; }
}
