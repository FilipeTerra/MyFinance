import { AccountType } from './AccountType';

export interface UpdateAccountRequestDto {
    name: string;
    type: AccountType;
}