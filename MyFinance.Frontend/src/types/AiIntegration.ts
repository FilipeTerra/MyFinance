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

export interface ProactiveInsightResponseDto {
    success: boolean;
    // Preenchido apenas quando success = false (ex: renda não cadastrada).
    message: string | null;
    curiosity: string | null;
    information: string | null;
    suggestion: string | null;
    hasAdequateReserve: boolean;
    alreadyHasReserveGoal: boolean;
    idealAmount: number;
    currentAmount: number;
    missingAmount: number;
    percentAchieved: number;
}