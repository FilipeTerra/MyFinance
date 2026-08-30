import type { ExpenseOverviewResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency, formatSignedCurrency, formatSignedPercent } from './gastosUtils';

interface GastosKpiRowProps {
    overview: ExpenseOverviewResponseDto;
}

/**
 * Faixa de KPIs do topo da aba Gastos. Reaproveita as classes `.dashboard-summary` /
 * `.summary-stat` já usadas em Metas e Investimentos (DashboardPage.css) — nenhum CSS novo.
 */
export function GastosKpiRow({ overview }: GastosKpiRowProps) {
    const maiorCategoria = overview.categories[0];
    // Para gastos, uma variação negativa (gastou menos) é boa notícia — daí a cor invertida
    // em relação ao "resultado" de investimentos.
    const variationTrend = overview.variationAmount < 0 ? 'green' : overview.variationAmount > 0 ? 'red' : 'flat';

    return (
        <div className="dashboard-summary">
            <div className="summary-stat">
                <span className="summary-stat-value">{formatCurrency(overview.totalExpenses)}</span>
                <span className="summary-stat-label">Total gasto no período</span>
            </div>
            <div className="summary-divider" />
            <div className="summary-stat">
                <span className="summary-stat-value">{formatCurrency(overview.monthlyAverage)}</span>
                <span className="summary-stat-label">Média mensal</span>
            </div>
            <div className="summary-divider" />
            <div className="summary-stat">
                <span className="summary-stat-value">{maiorCategoria ? maiorCategoria.categoryName : '—'}</span>
                <span className="summary-stat-label">
                    {maiorCategoria ? `Maior categoria (${formatCurrency(maiorCategoria.total)})` : 'Maior categoria'}
                </span>
            </div>
            <div className="summary-divider summary-divider--grow" />
            <div className="summary-stat summary-stat--right">
                <span className={`summary-stat-value summary-stat-value--${variationTrend}`}>
                    {formatSignedCurrency(overview.variationAmount)}
                </span>
                <span className="summary-stat-label">
                    vs. período anterior ({formatSignedPercent(overview.variationPercent)})
                </span>
            </div>
        </div>
    );
}
