import type { InvestmentType } from './InvestmentType';

export interface CotacaoPontoDto {
    data: string;
    valor: number;
}

export interface InvestimentoResponseDto {
    id: string;
    nome: string;
    /** Soma de todo o dinheiro já aportado neste investimento (aporte inicial + aportes adicionais). */
    totalAportado: number;
    valorAtual: number;
    tipo: InvestmentType;
    dataCriacao: string;
    rentabilidadePercentual: number;
    ticker?: string | null;
    /** Variação percentual da cotação nos últimos 3 meses. Nulo quando não há histórico suficiente. */
    variacaoUltimos3MesesPercentual: number | null;
    /** Série de cotações dos últimos 3 meses, usada para o mini-gráfico do card. */
    historicoCotacoes: CotacaoPontoDto[];
}

export interface CreateInvestimentoRequestDto {
    nome: string;
    valorInicial: number;
    tipo: InvestmentType;
    /** Conta de origem que será debitada no valor do aporte inicial. */
    accountId: string;
    /** Categoria da transação de origem. */
    categoryId: string;
    /** Código do ativo na B3 (ex: "PETR4"), usado para buscar cotações automaticamente. */
    ticker?: string;
}

export interface AporteInvestimentoRequestDto {
    valor: number;
    /** Conta de origem que será debitada no valor do aporte. */
    accountId: string;
    /** Categoria da transação gerada pelo aporte. */
    categoryId: string;
    /** Data do aporte (ISO). Quando omitida, assume a data/hora atual. */
    data?: string;
}

export interface AporteHistoricoResponseDto {
    transactionId: string;
    valor: number;
    data: string;
    contaNome?: string | null;
}
