// src/types/AiIntegration.ts

export interface AiTransactionResponseDto {
    date: string;
    description: string;
    amount: number;
    accountId: string;
    categoryId: string | null;
    suggestedCategoryName: string | null;
    isSuggestion: boolean;
}

export interface SaveBatchTransactionRequestDto {
    date: string;
    description: string;
    amount: number;
    accountId: string;
    categoryId: string | null;
    newCategoryName: string | null;
    isNewCategory: boolean;
}