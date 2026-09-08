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
    // Arquivo de origem, quando a importação foi de vários arquivos de uma vez.
    sourceFileName: string | null;
}

/** Resultado da leitura de um arquivo dentro de um lote de importação. */
export interface FileImportSummaryDto {
    fileName: string;
    success: boolean;
    // Motivo da falha. Preenchido só quando success é falso.
    message: string | null;
    parserUsed: string | null;
    aiUsed: boolean;
    transactionCount: number;
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
    // Nome do parser que leu o arquivo, ou "IA" quando veio do agente. Só vem
    // preenchido quando exatamente um arquivo teve sucesso — com vários, o
    // detalhe por arquivo vive em `files`.
    parserUsed: string | null;
    aiUsed: boolean;
    // A IA foi consultada e não respondeu; a importação seguiu sem ela.
    aiUnavailable: boolean;
    // Quantas transações do arquivo já existem na conta.
    duplicateCount: number;
    warnings: string[];
    // Resultado de cada arquivo do lote, sucesso ou falha.
    files: FileImportSummaryDto[];
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

/** Problema apontado pela API numa linha específica do lote enviado. */
export interface BatchLineError {
    // Posição na lista ENVIADA (só as linhas marcadas), não na tabela da tela.
    index: number;
    description: string;
    message: string;
}

/** Resposta de POST /transactions/batch, tanto no sucesso quanto no 400. */
export interface SaveBatchResponse {
    message: string;
    savedCount?: number;
    errors: BatchLineError[];
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