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

export type ProactiveInsightCardType = 'aviso' | 'info';

export interface ProactiveInsightResponseDto {
    success: boolean;
    // Preenchido apenas quando success = false (ex: renda não cadastrada).
    message: string | null;
    // Decisão de exibição já resolvida no backend: false = reserva adequada, não mostrar nada.
    showCard: boolean;
    // "aviso" (já investe, mas não atingiu o ideal) ou "info" (ainda não iniciou).
    cardType: ProactiveInsightCardType | null;
    curiosity: string | null;
    information: string | null;
    suggestion: string | null;
    idealAmount: number;
    currentAmount: number;
    missingAmount: number;
    percentAchieved: number;
}

export interface LifestyleInsightResponseDto {
    success: boolean;
    // Preenchido apenas quando success = false (ex: renda não cadastrada).
    message: string | null;
    alert: boolean;
    curiosity: string | null;
    information: string | null;
    suggestion: string | null;
    lifestylePercentOfIncome: number | null;
    lifestyleGrowthPercent: number | null;
    investmentGrowthPercent: number | null;
}