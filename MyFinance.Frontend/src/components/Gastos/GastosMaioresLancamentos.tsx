import type { ExpenseOverviewResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency } from './gastosUtils';
import { Card, EstadoVazio } from '../Shared/ui';
import './GastosMaioresLancamentos.css';

interface GastosMaioresLancamentosProps {
    overview: ExpenseOverviewResponseDto;
}

/**
 * Extraído do antigo GastosFluxoMensal — antes esta lista vivia dentro do
 * mesmo card do gráfico de fluxo (o que inflava aquele card e desbalanceava
 * o grid) e sumia sem aviso quando vazia, mudando a altura do card entre
 * períodos. Agora é seu próprio card, com estado vazio explícito.
 */
export function GastosMaioresLancamentos({ overview }: GastosMaioresLancamentosProps) {
    return (
        <Card>
            <h3 className="gastos-card-title">Maiores lançamentos do período</h3>
            {overview.topExpenses.length === 0 ? (
                <EstadoVazio variante="inline" icone="🧾" titulo="Nenhum lançamento em destaque neste período" />
            ) : (
                <ul className="fluxo-top-expenses-list">
                    {overview.topExpenses.map(despesa => (
                        <li key={despesa.id} className="fluxo-top-expense-item">
                            <div className="fluxo-top-expense-info">
                                <span className="fluxo-top-expense-desc">{despesa.description}</span>
                                <span className="fluxo-top-expense-meta">
                                    {despesa.categoryName} · {despesa.accountName} ·{' '}
                                    {new Date(despesa.date).toLocaleDateString('pt-BR', { timeZone: 'UTC' })}
                                </span>
                            </div>
                            <span className="fluxo-top-expense-valor">{formatCurrency(despesa.amount)}</span>
                        </li>
                    ))}
                </ul>
            )}
        </Card>
    );
}
