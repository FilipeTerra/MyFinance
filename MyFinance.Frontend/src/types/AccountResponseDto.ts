import type { AccountType } from './AccountType';

export interface AccountResponseDto {
    id: string;
    name: string;
    typeName: string;
    type: AccountType;
    initialBalance: number;
    currentBalance: number;
    userId: string;
}