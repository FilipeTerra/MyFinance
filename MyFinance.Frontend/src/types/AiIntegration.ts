// src/types/AiIntegration.ts

export interface AiTransactionResponseDto {
    date: string;
    description: string;
    amount: number;
    accountId: string;
    categoryId: string | null;
    suggestedCategoryName: string | null;
    isSuggestion: boolean;
    // Um lançamento igual já existe na conta. A linha continua na lista, mas
    // chega desmarcada na revisão.
    isDuplicate: boolean;
}

/**
 * Resposta da importação de extrato. A leitura é determinística na API; a IA só
 * entra quando nenhum parser reconhece o arquivo ou para sugerir categoria ao
 * que sobrou — por isso o resultado diz o que de fato aconteceu.
 */
export interface StatementImportResultDto {
    success: boolean;
    // Preenchido apenas quando success = false.
    message: string | null;
    transactions: AiTransactionResponseDto[];
    // Nome do parser que leu o arquivo, ou "IA" quando veio do agente.
    parserUsed: string | null;
    aiUsed: boolean;
    // A IA foi consultada e não respondeu; a importação seguiu sem ela.
    aiUnavailable: boolean;
    // Quantas transações do arquivo já existem na conta.
    duplicateCount: number;
    warnings: string[];
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