/**
 * De onde vem a taxa de juros anual simulada.
 * Corresponde ao enum FonteTaxaJuros.cs no backend (serializado como número).
 */
export const FonteTaxaJuros = {
    Manual: 1,
    Selic: 2,
    PercentualCdi: 3,
} as const;

export type FonteTaxaJuros = (typeof FonteTaxaJuros)[keyof typeof FonteTaxaJuros];
