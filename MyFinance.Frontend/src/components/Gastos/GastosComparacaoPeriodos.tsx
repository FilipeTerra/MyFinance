import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Cell, ReferenceLine, ResponsiveContainer } from 'recharts';
import type { CategoryExpenseDto, ExpenseOverviewResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency, formatSignedCurrency, formatSignedPercent } from './gastosUtils';
import './GastosComparacaoPeriodos.css';

interface GastosComparacaoPeriodosProps {
    overview: ExpenseOverviewResponseDto;
}

interface ComparacaoItem {
    categoryId: string;
    categoryName: string;
    atual: number;
    anterior: number;
    delta: number;
    deltaPercent: number | null;
}

// Queda de gasto é boa notícia (verde); alta é má notícia (vermelho) — mesma convenção de
// cor usada no restante do dashboard para variações favoráveis/desfavoráveis.
const VERDE_QUEDA = '#10b981';
const VERMELHO_ALTA = '#ef4444';
const MAX_ITENS_GRAFICO = 8;

function construirComparacao(atual: CategoryExpenseDto[], anterior: CategoryExpenseDto[]): ComparacaoItem[] {
    const porId = new Map<string, ComparacaoItem>();
    atual.forEach(c => {
        porId.set(c.categoryId, { categoryId: c.categoryId, categoryName: c.categoryName, atual: c.total, anterior: 0, delta: 0, deltaPercent: null });
    });
    anterior.forEach(c => {
        const existente = porId.get(c.categoryId);
        if (existente) existente.anterior = c.total;
        else porId.set(c.categoryId, { categoryId: c.categoryId, categoryName: c.categoryName, atual: 0, anterior: c.total, delta: 0, deltaPercent: null });
    });

    return [...porId.values()]
        .map(item => ({
            ...item,
            delta: item.atual - item.anterior,
            deltaPercent: item.anterior === 0 ? null : ((item.atual - item.anterior) / item.anterior) * 100,
        }))
        .sort((a, b) => Math.abs(b.delta) - Math.abs(a.delta));
}

/**
 * Compara o gasto por categoria do período atual com o período anterior de mesma duração.
 * Barras divergentes a partir de zero — queda em verde, alta em vermelho — com as maiores
 * variações destacadas acima do gráfico.
 */
export function GastosComparacaoPeriodos({ overview }: GastosComparacaoPeriodosProps) {
    const comparacao = construirComparacao(overview.categories, overview.previousCategories);

    if (comparacao.length === 0) {
        return (
            <div className="gastos-card">
                <h3 className="gastos-card-title">Comparação com o período anterior</h3>
                <p className="gastos-card-empty">Sem dados suficientes para comparar períodos.</p>
            </div>
        );
    }

    const maiorAlta = [...comparacao].filter(i => i.delta > 0).sort((a, b) => b.delta - a.delta)[0];
    const maiorQueda = [...comparacao].filter(i => i.delta < 0).sort((a, b) => a.delta - b.delta)[0];
    const itens = comparacao.slice(0, MAX_ITENS_GRAFICO).sort((a, b) => b.delta - a.delta);

    return (
        <div className="gastos-card">
            <h3 className="gastos-card-title">Comparação com o período anterior</h3>

            {(maiorAlta || maiorQueda) && (
                <div className="comparacao-destaques">
                    {maiorAlta && (
                        <div className="comparacao-destaque comparacao-destaque--alta">
                            <span className="comparacao-destaque-label">Maior alta</span>
                            <span className="comparacao-destaque-nome">{maiorAlta.categoryName}</span>
                            <span className="comparacao-destaque-valor comparacao-destaque-valor--alta">
                                {formatSignedCurrency(maiorAlta.delta)} ({formatSignedPercent(maiorAlta.deltaPercent)})
                            </span>
                        </div>
                    )}
                    {maiorQueda && (
                        <div className="comparacao-destaque comparacao-destaque--queda">
                            <span className="comparacao-destaque-label">Maior queda</span>
                            <span className="comparacao-destaque-nome">{maiorQueda.categoryName}</span>
                            <span className="comparacao-destaque-valor comparacao-destaque-valor--queda">
                                {formatSignedCurrency(maiorQueda.delta)} ({formatSignedPercent(maiorQueda.deltaPercent)})
                            </span>
                        </div>
                    )}
                </div>
            )}

            <ResponsiveContainer width="100%" height={Math.max(180, itens.length * 42)}>
                <BarChart data={itens} layout="vertical" margin={{ top: 8, right: 24, left: 8, bottom: 0 }}>
                    <CartesianGrid stroke="#e2e8f0" horizontal={false} />
                    <XAxis type="number" tickFormatter={(v: number) => formatCurrency(v)} stroke="#94a3b8" fontSize={12} />
                    <YAxis type="category" dataKey="categoryName" width={110} stroke="#94a3b8" fontSize={12} />
                    <ReferenceLine x={0} stroke="#cbd5e1" />
                    <Tooltip
                        formatter={(value, _name, entry) => {
                            const item = (entry as { payload?: ComparacaoItem })?.payload;
                            return [`${formatSignedCurrency(Number(value))} (${formatSignedPercent(item?.deltaPercent ?? null)})`, 'Variação'];
                        }}
                    />
                    <Bar dataKey="delta" maxBarSize={20}>
                        {itens.map(item => (
                            <Cell key={item.categoryId} fill={item.delta <= 0 ? VERDE_QUEDA : VERMELHO_ALTA} />
                        ))}
                    </Bar>
                </BarChart>
            </ResponsiveContainer>
        </div>
    );
}
