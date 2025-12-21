import { AccountType } from './AccountType';

export interface AccountRequestDto {
    name: string;
    type: AccountType;
    initialBalance: number;
}