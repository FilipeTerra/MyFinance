/**
 * Como o aporte mensal recorrente é reajustado a cada 12 meses simulados.
 * Corresponde ao enum ReajusteAporteModo.cs no backend (serializado como número).
 */
export const ReajusteAporteModo = {
    Nenhum: 1,
    PercentualFixo: 2,
    Ipca: 3,
} as const;

export type ReajusteAporteModo = (typeof ReajusteAporteModo)[keyof typeof ReajusteAporteModo];
