import { InvestmentType } from '../../types/InvestmentType';

export interface InvestmentTypeMeta {
    label: string;
    icon: string;
    /** Cor de destaque do tipo — usada em chips, badges e no card. */
    color: string;
}

/**
 * Metadados de apresentação para cada classe de ativo.
 * Centralizado para manter card, modal e badges visualmente consistentes.
 */
export const INVESTMENT_TYPE_META: Record<InvestmentType, InvestmentTypeMeta> = {
    [InvestmentType.RendaFixa]: { label: 'Renda Fixa', icon: '🏦', color: '#0ea5e9' },
    [InvestmentType.Acao]:      { label: 'Ação',        icon: '📈', color: '#8b5cf6' },
    [InvestmentType.FII]:       { label: 'FII',         icon: '🏢', color: '#f59e0b' },
    [InvestmentType.Cripto]:    { label: 'Cripto',      icon: '₿',  color: '#f97316' },
    [InvestmentType.ETF]:       { label: 'ETF',         icon: '🧺', color: '#10b981' },
};
