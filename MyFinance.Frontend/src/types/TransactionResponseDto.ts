import type { TransactionType } from './TransactionType';

export interface TransactionResponseDto {
    id: string;
    description: string;
    amount: number;
    type: TransactionType;
    typeName: string; // "Income" ou "Expense"
    date: string; // (string no JSON, pode ser convertida para Date)
    createdAt: string;
    accountId: string;
    accountName: string;
}