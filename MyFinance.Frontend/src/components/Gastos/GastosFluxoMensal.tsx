import { ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ExpenseOverviewResponseDto, ExpenseTimelineResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency, formatMesLabel, formatMesLabelCompleto } from './gastosUtils';
import './GastosFluxoMensal.css';

interface GastosFluxoMensalProps {
    timeline: ExpenseTimelineResponseDto;
    overview: ExpenseOverviewResponseDto;
}

// Receita segue o azul primário já usado nos gráficos da Calculadora; despesa segue o vermelho
// já usado em todo o dashboard para valores negativos; saldo é uma linha neutra (mesmo padrão
// tracejado cinza usado para "Total aportado" em ResultadoProjecaoDetalhado).
const AZUL_RECEITA = '#3b82f6';
const VERMELHO_DESPESA = '#ef4444';
const CINZA_SALDO = '#64748b';

/**
 * Fluxo mensal de receitas x despesas com linha de saldo, seguido dos maiores lançamentos
 * individuais de despesa do período selecionado.
 */
export function GastosFluxoMensal({ timeline, overview }: GastosFluxoMensalProps) {
    const dados = timeline.months.map(mes => ({
        label: mes.label,
        mesCompleto: formatMesLabelCompleto(mes.label),
        receita: mes.totalIncome,
        despesa: mes.totalExpenses,
        saldo: mes.balance,
    }));

    const xAxisInterval = Math.max(0, Math.ceil(dados.length / 10) - 1);
    const semDados = dados.every(d => d.receita === 0 && d.despesa === 0);

    return (
        <div className="gastos-card">
            <h3 className="gastos-card-title">Fluxo mensal — receitas x despesas</h3>

            {semDados ? (
                <p className="gastos-card-empty">Nenhuma movimentação encontrada no período selecionado.</p>
            ) : (
                <ResponsiveContainer width="100%" height={280}>
                    <ComposedChart data={dados} margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
                        <CartesianGrid stroke="#e2e8f0" vertical={false} />
                        <XAxis dataKey="label" tickFormatter={formatMesLabel} interval={xAxisInterval} stroke="#94a3b8" fontSize={12} />
                        <YAxis tickFormatter={(v: number) => formatCurrency(v)} width={90} stroke="#94a3b8" fontSize={12} />
                        <Tooltip
                            formatter={(value, name) => [formatCurrency(Number(value)), name]}
                            labelFormatter={(_label, payload) => (payload?.[0]?.payload as { mesCompleto?: string })?.mesCompleto ?? ''}
                        />
                        <Legend />
                        <Bar dataKey="receita" name="Receitas" fill={AZUL_RECEITA} maxBarSize={20} radius={[4, 4, 0, 0]} />
                        <Bar dataKey="despesa" name="Despesas" fill={VERMELHO_DESPESA} maxBarSize={20} radius={[4, 4, 0, 0]} />
                        <Line type="monotone" dataKey="saldo" name="Saldo" stroke={CINZA_SALDO} strokeWidth={2} strokeDasharray="4 4" dot={false} />
                    </ComposedChart>
                </ResponsiveContainer>
            )}

            {overview.topExpenses.length > 0 && (
                <div className="fluxo-top-expenses">
                    <h4 className="fluxo-top-expenses-title">Maiores lançamentos do período</h4>
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
                </div>
            )}
        </div>
    );
}
