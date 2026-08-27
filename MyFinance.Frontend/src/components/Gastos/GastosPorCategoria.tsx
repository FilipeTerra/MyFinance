import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from 'recharts';
import type { ExpenseOverviewResponseDto } from '../../types/ExpenseAnalytics';
import { agruparTopCategorias, corDaCategoria, formatCurrency, COR_OUTRAS } from './gastosUtils';
import './GastosPorCategoria.css';

interface GastosPorCategoriaProps {
    overview: ExpenseOverviewResponseDto;
    coresCategorias: Map<string, string>;
}

/**
 * Maiores gastos por categoria: donut com participação % (top 5 + "Outras", limite de 6 fatias
 * para permanecer legível num relance) ao lado do ranking completo, que funciona como legenda
 * (cada linha carrega a mesma cor da fatia) e como "tabela" — nenhum valor fica só no gráfico.
 */
export function GastosPorCategoria({ overview, coresCategorias }: GastosPorCategoriaProps) {
    const categorias = overview.categories;

    if (categorias.length === 0) {
        return (
            <div className="gastos-card">
                <h3 className="gastos-card-title">Gastos por categoria</h3>
                <p className="gastos-card-empty">Nenhuma despesa encontrada no período selecionado.</p>
            </div>
        );
    }

    const agrupadas = agruparTopCategorias(categorias);

    return (
        <div className="gastos-card">
            <h3 className="gastos-card-title">Gastos por categoria</h3>
            <div className="categoria-layout">
                <div className="categoria-donut">
                    <ResponsiveContainer width="100%" height={220}>
                        <PieChart>
                            <Pie
                                data={agrupadas}
                                dataKey="total"
                                nameKey="categoryName"
                                innerRadius={60}
                                outerRadius={95}
                                paddingAngle={2}
                                stroke="#ffffff"
                                strokeWidth={2}
                            >
                                {agrupadas.map(categoria => (
                                    <Cell
                                        key={categoria.categoryId}
                                        fill={categoria.isOutras ? COR_OUTRAS : corDaCategoria(coresCategorias, categoria.categoryId)}
                                    />
                                ))}
                            </Pie>
                            <Tooltip formatter={(value, name) => [formatCurrency(Number(value)), name]} />
                        </PieChart>
                    </ResponsiveContainer>
                    <div className="categoria-donut-total" aria-hidden="true">
                        <span className="categoria-donut-total-value">{formatCurrency(overview.totalExpenses)}</span>
                        <span className="categoria-donut-total-label">total</span>
                    </div>
                </div>

                <ol className="categoria-ranking">
                    {categorias.map(categoria => (
                        <li key={categoria.categoryId} className="categoria-ranking-item">
                            <span
                                className="categoria-ranking-dot"
                                style={{ background: corDaCategoria(coresCategorias, categoria.categoryId) }}
                                aria-hidden="true"
                            />
                            <div className="categoria-ranking-info">
                                <div className="categoria-ranking-row">
                                    <span className="categoria-ranking-name">{categoria.categoryName}</span>
                                    <span className="categoria-ranking-value">{formatCurrency(categoria.total)}</span>
                                </div>
                                <div className="categoria-ranking-bar-track">
                                    <div
                                        className="categoria-ranking-bar-fill"
                                        style={{
                                            width: `${categoria.percentage}%`,
                                            background: corDaCategoria(coresCategorias, categoria.categoryId),
                                        }}
                                    />
                                </div>
                                <span className="categoria-ranking-meta">
                                    {categoria.percentage.toFixed(1)}% · {categoria.transactionCount}{' '}
                                    {categoria.transactionCount === 1 ? 'lançamento' : 'lançamentos'}
                                </span>
                            </div>
                        </li>
                    ))}
                </ol>
            </div>
        </div>
    );
}
