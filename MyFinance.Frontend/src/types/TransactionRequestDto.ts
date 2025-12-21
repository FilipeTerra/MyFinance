import { TransactionType } from './TransactionType';

export interface TransactionRequestDto {
    description: string;
    amount: number;
    type: TransactionType;
    date: string;
    accountId: string;
    categoryId: string;
}