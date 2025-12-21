/**
 * Define os tipos de contas financeiras que o usuário pode cadastrar.
 * Corresponde ao enum AccountType.cs no backend.
 */
export const AccountType = {
    ContaCorrente: 1,
    Poupanca: 2,
    Carteira: 3,
    CartaoCredito: 4, 
    Investimento: 5, 
    Outro: 6 
} as const;

export type AccountType = (typeof AccountType)[keyof typeof AccountType];