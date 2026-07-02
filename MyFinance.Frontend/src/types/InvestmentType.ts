/**
 * Classes de ativos de investimento.
 * Corresponde ao enum InvestmentType.cs no backend (serializado como número).
 */
export const InvestmentType = {
    RendaFixa: 1,
    Acao: 2,
    FII: 3,
    Cripto: 4,
    ETF: 5,
} as const;

export type InvestmentType = (typeof InvestmentType)[keyof typeof InvestmentType];
