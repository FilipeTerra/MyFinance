/** Filtros aceitos pelos endpoints de análise de gastos (api/analytics/expenses/*). */
export interface ExpenseAnalyticsFilterParams {
    startDate?: string;
    endDate?: string;
    accountId?: string;
    /** Usado apenas por getExpenseTimeline. */
    months?: number;
}

/** Total de despesas de uma categoria em um período. */
export interface CategoryExpenseDto {
    categoryId: string;
    categoryName: string;
    total: number;
    percentage: number;
    transactionCount: number;
}

/** Um lançamento individual de despesa em destaque. */
export interface TopExpenseDto {
    id: string;
    description: string;
    amount: number;
    date: string;
    categoryName: string;
    accountName: string;
}

/** Visão geral de despesas/receitas de um período, com comparação ao período anterior. */
export interface ExpenseOverviewResponseDto {
    startDate: string;
    endDate: string;
    totalExpenses: number;
    totalIncome: number;
    balance: number;
    transactionCount: number;
    monthlyAverage: number;
    previousTotalExpenses: number;
    variationAmount: number;
    variationPercent: number | null;
    categories: CategoryExpenseDto[];
    previousCategories: CategoryExpenseDto[];
    topExpenses: TopExpenseDto[];
}

/** Ponto mensal da linha do tempo de gastos. */
export interface MonthlyPointDto {
    year: number;
    month: number;
    /** Formato "yyyy-MM", ex.: "2026-08". */
    label: string;
    totalExpenses: number;
    totalIncome: number;
    balance: number;
    categories: CategoryExpenseDto[];
}

/** Evolução mensal de gastos, em ordem cronológica crescente e sem lacunas. */
export interface ExpenseTimelineResponseDto {
    months: MonthlyPointDto[];
}
