import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from 'recharts';
import type { ExpenseOverviewResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency } from './gastosUtils';
import { corDaCategoria, type RankingCategorias } from './gastosSelectors';
import { COR_NEUTRA } from '../Shared/charts/chartTheme';
import { Card, Colapsavel, EstadoVazio } from '../Shared/ui';
import { ChartFigure } from '../Shared/charts/ChartFigure';
import './GastosPorCategoria.css';

interface GastosPorCategoriaProps {
    overview: ExpenseOverviewResponseDto;
    ranking: RankingCategorias;
}

/**
 * Maiores gastos por categoria: donut (top 5 + "Outras") ao lado do ranking
 * das mesmas 5 categorias — o donut e o ranking agora mostram o MESMO corte
 * (antes cada um vinha de uma fonte própria). O restante das categorias fica
 * recolhido, sem cor de identidade própria — antes a 7ª categoria em diante
 * ganhava o mesmo cinza da fatia "Outras", fazendo parecer cor de categoria.
 */
export function GastosPorCategoria({ overview, ranking }: GastosPorCategoriaProps) {
    const { principais, outras, temOutras, totalOutras, transacoesOutras } = ranking;

    if (principais.length === 0) {
        return (
            <Card>
                <h3 className="gastos-card-title">Para onde foi seu dinheiro</h3>
                <EstadoVazio variante="inline" icone="📭" titulo="Nenhuma despesa no período" />
            </Card>
        );
    }

    const percentualOutras = overview.totalExpenses > 0 ? (totalOutras / overview.totalExpenses) * 100 : 0;
    const dadosDonut = temOutras
        ? [...principais, { categoryId: '__outras__', categoryName: 'Outras', total: totalOutras, percentage: percentualOutras, transactionCount: transacoesOutras }]
        : principais;

    return (
        <Card>
            <div className="categoria-header">
                <h3 className="gastos-card-title">Para onde foi seu dinheiro</h3>
                <span className="categoria-header-total">{formatCurrency(overview.totalExpenses)} no período</span>
            </div>

            <div className="categoria-layout">
                <div className="categoria-donut">
                    <ChartFigure
                        titulo="Gastos por categoria"
                        descricao={`Distribuição de ${formatCurrency(overview.totalExpenses)} em despesas entre as categorias do período`}
                        dadosTabela={{
                            colunas: ['Categoria', 'Total', 'Participação'],
                            linhas: dadosDonut.map(c => [c.categoryName, formatCurrency(c.total), `${c.percentage.toFixed(1)}%`]),
                        }}
                    >
                        <ResponsiveContainer width="100%" height={220}>
                            <PieChart>
                                <Pie
                                    data={dadosDonut}
                                    dataKey="total"
                                    nameKey="categoryName"
                                    innerRadius={60}
                                    outerRadius={95}
                                    paddingAngle={2}
                                    stroke="var(--color-surface)"
                                    strokeWidth={2}
                                >
                                    {dadosDonut.map(categoria => (
                                        <Cell
                                            key={categoria.categoryId}
                                            fill={categoria.categoryId === '__outras__' ? COR_NEUTRA : corDaCategoria(ranking, categoria.categoryId)}
                                        />
                                    ))}
                                </Pie>
                                <Tooltip formatter={(value, name) => [formatCurrency(Number(value)), name]} />
                            </PieChart>
                        </ResponsiveContainer>
                    </ChartFigure>
                    <div className="categoria-donut-total" aria-hidden="true">
                        <span className="categoria-donut-total-value">{formatCurrency(overview.totalExpenses)}</span>
                        <span className="categoria-donut-total-label">total</span>
                    </div>
                </div>

                <ol className="categoria-ranking">
                    {principais.map(categoria => (
                        <li key={categoria.categoryId} className="categoria-ranking-item">
                            <span
                                className="categoria-ranking-dot"
                                style={{ background: corDaCategoria(ranking, categoria.categoryId) }}
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
                                        style={{ width: `${categoria.percentage}%`, background: corDaCategoria(ranking, categoria.categoryId) }}
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

            {temOutras && (
                <Colapsavel titulo="Outras categorias" selo={`${outras.length} · ${formatCurrency(totalOutras)}`}>
                    <ul className="categoria-outras-lista">
                        {outras.map(categoria => (
                            <li key={categoria.categoryId} className="categoria-outras-item">
                                <span className="categoria-outras-nome">{categoria.categoryName}</span>
                                <span className="categoria-outras-valor">{formatCurrency(categoria.total)}</span>
                            </li>
                        ))}
                    </ul>
                </Colapsavel>
            )}
        </Card>
    );
}
