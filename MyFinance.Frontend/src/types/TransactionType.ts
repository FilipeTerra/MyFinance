/**
 * Define os tipos de transações financeiras (Entrada ou Saída).
 * Substitui o enum tradicional por objeto constante para compatibilidade.
 */
export const TransactionType = {
    Income: 1, // Receita
    Expense: 2 // Despesa
} as const;

export type TransactionType = (typeof TransactionType)[keyof typeof TransactionType];