/** Sistema de amortização de um financiamento. Espelha o enum do backend. */
export const SistemaAmortizacao = {
    Price: 1,
    Sac: 2
} as const;
export type SistemaAmortizacao = (typeof SistemaAmortizacao)[keyof typeof SistemaAmortizacao];

/** Rótulo de UI de cada sistema. */
export const ROTULO_SISTEMA: Record<SistemaAmortizacao, string> = {
    [SistemaAmortizacao.Price]: 'Price',
    [SistemaAmortizacao.Sac]: 'SAC'
};

/** O que um pagamento extra abate no contrato. */
export const ModoAmortizacaoExtra = {
    ReduzirPrazo: 1,
    ReduzirParcela: 2
} as const;
export type ModoAmortizacaoExtra = (typeof ModoAmortizacaoExtra)[keyof typeof ModoAmortizacaoExtra];

/** O que compensa mais financeiramente entre amortizar uma dívida a mais ou investir o mesmo dinheiro. */
export const RecomendacaoFinanceira = {
    Amortizar: 1,
    Investir: 2
} as const;
export type RecomendacaoFinanceira = (typeof RecomendacaoFinanceira)[keyof typeof RecomendacaoFinanceira];

export const ROTULO_RECOMENDACAO: Record<RecomendacaoFinanceira, string> = {
    [RecomendacaoFinanceira.Amortizar]: 'Compensa mais amortizar',
    [RecomendacaoFinanceira.Investir]: 'Compensa mais investir'
};
