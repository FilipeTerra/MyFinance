namespace MyFinance.Domain.Enums;

/// <summary>
/// Define as classes de ativos suportadas pela gestão de investimentos.
/// </summary>
public enum InvestmentType
{
    /// <summary>
    /// Renda Fixa (ex: Tesouro Direto, CDB, LCI/LCA).
    /// </summary>
    RendaFixa = 1,

    /// <summary>
    /// Ações negociadas em bolsa (ex: PETR4, VALE3).
    /// </summary>
    Acao = 2,

    /// <summary>
    /// Fundos de Investimento Imobiliário (ex: HGLG11, MXRF11).
    /// </summary>
    FII = 3,

    /// <summary>
    /// Criptoativos (ex: Bitcoin, Ethereum).
    /// </summary>
    Cripto = 4,

    /// <summary>
    /// Exchange Traded Funds (ex: BOVA11, IVVB11).
    /// </summary>
    ETF = 5
}
