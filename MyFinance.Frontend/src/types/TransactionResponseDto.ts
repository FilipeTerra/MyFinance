import type { TransactionType } from './TransactionType';

export interface TransactionResponseDto {
    id: string;
    description: string;
    amount: number;
    type: TransactionType;
    typeName: string; // "Income" ou "Expense"
    date: string;
    createdAt: string;
    accountId: string;
    accountName: string;
    categoryId: string;
    categoryName: string;
}