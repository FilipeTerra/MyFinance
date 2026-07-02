import type { InvestmentType } from './InvestmentType';

export interface InvestimentoResponseDto {
    id: string;
    nome: string;
    valorInicial: number;
    valorAtual: number;
    tipo: InvestmentType;
    dataCriacao: string;
    rentabilidadePercentual: number;
}

export interface CreateInvestimentoRequestDto {
    nome: string;
    valorInicial: number;
    tipo: InvestmentType;
    /** Conta de origem que será debitada no valor do aporte inicial. */
    accountId: string;
    /** Categoria da transação de origem. */
    categoryId: string;
}
