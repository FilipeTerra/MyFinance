/**
 * Quão cortável é o gasto de uma categoria. Espelha MyFinance.Domain.Enums.ExpenseNature.
 * A classificação é sempre do usuário — o sistema nunca adivinha.
 */
export const ExpenseNature = {
    /** Estado inicial. Nunca entra em um plano de corte. */
    NaoClassificado: 0,
    /** Intocável (moradia, saúde, transporte de trabalho). */
    Essencial: 1,
    /** O usuário aceita reduzir para acelerar uma meta. */
    Discricionario: 2,
} as const;

export type ExpenseNature = (typeof ExpenseNature)[keyof typeof ExpenseNature];
