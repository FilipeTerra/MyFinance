import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Cell, LabelList, ReferenceLine, ResponsiveContainer } from 'recharts';
import type { CategoryExpenseDto, ExpenseOverviewResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency, formatSignedCurrency, formatSignedPercent } from './gastosUtils';
import { COR_ALTA, COR_QUEDA } from '../Shared/charts/chartTheme';
import { ChartFigure } from '../Shared/charts/ChartFigure';
import { EstadoVazio } from '../Shared/ui';
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

const rotuloDelta = (item: ComparacaoItem) => `${item.delta <= 0 ? '▼' : '▲'} ${formatSignedCurrency(item.delta)}`;

interface LabelContentProps {
    x?: number | string;
    y?: number | string;
    width?: number | string;
    height?: number | string;
    index?: number;
}

/**
 * Compara o gasto por categoria do período atual com o período anterior de
 * mesma duração. Antes a direção (alta/queda) só existia como cor da barra
 * — agora cada barra também carrega um rótulo com valor e seta, então a
 * informação não depende de distinguir vermelho de verde.
 */
export function GastosComparacaoPeriodos({ overview }: GastosComparacaoPeriodosProps) {
    const comparacao = construirComparacao(overview.categories, overview.previousCategories);

    if (comparacao.length === 0) {
        return <EstadoVazio variante="inline" icone="⚖️" titulo="Sem dados suficientes para comparar períodos" />;
    }

    const maiorAlta = [...comparacao].filter(i => i.delta > 0).sort((a, b) => b.delta - a.delta)[0];
    const maiorQueda = [...comparacao].filter(i => i.delta < 0).sort((a, b) => a.delta - b.delta)[0];
    const itens = comparacao.slice(0, MAX_ITENS_GRAFICO).sort((a, b) => b.delta - a.delta);

    return (
        <div className="comparacao-conteudo">
            {(maiorAlta || maiorQueda) && (
                <div className="comparacao-destaques">
                    {maiorAlta && (
                        <div className="comparacao-destaque comparacao-destaque--alta">
                            <span className="comparacao-destaque-label">▲ Maior alta</span>
                            <span className="comparacao-destaque-nome">{maiorAlta.categoryName}</span>
                            <span className="comparacao-destaque-valor comparacao-destaque-valor--alta">
                                {formatSignedCurrency(maiorAlta.delta)} ({formatSignedPercent(maiorAlta.deltaPercent)})
                            </span>
                        </div>
                    )}
                    {maiorQueda && (
                        <div className="comparacao-destaque comparacao-destaque--queda">
                            <span className="comparacao-destaque-label">▼ Maior queda</span>
                            <span className="comparacao-destaque-nome">{maiorQueda.categoryName}</span>
                            <span className="comparacao-destaque-valor comparacao-destaque-valor--queda">
                                {formatSignedCurrency(maiorQueda.delta)} ({formatSignedPercent(maiorQueda.deltaPercent)})
                            </span>
                        </div>
                    )}
                </div>
            )}

            <ChartFigure
                titulo="Comparação com o período anterior"
                descricao="Variação de gasto por categoria em relação ao período anterior de mesma duração"
                altura={Math.max(180, itens.length * 42)}
                dadosTabela={{
                    colunas: ['Categoria', 'Período atual', 'Período anterior', 'Variação'],
                    linhas: itens.map(i => [i.categoryName, formatCurrency(i.atual), formatCurrency(i.anterior), `${formatSignedCurrency(i.delta)} (${formatSignedPercent(i.deltaPercent)})`]),
                }}
            >
                <ResponsiveContainer width="100%" height={Math.max(180, itens.length * 42)}>
                    <BarChart data={itens} layout="vertical" margin={{ top: 8, right: 64, left: 64, bottom: 0 }}>
                        <CartesianGrid stroke="#e2e8f0" horizontal={false} />
                        <XAxis type="number" tickFormatter={(v: number) => formatCurrency(v)} stroke="#94a3b8" fontSize={12} tick={{ fill: '#64748b' }} />
                        <YAxis type="category" dataKey="categoryName" width={110} stroke="#94a3b8" fontSize={12} tick={{ fill: '#64748b' }} />
                        <ReferenceLine x={0} stroke="#cbd5e1" />
                        <Tooltip
                            formatter={(value, _name, entry) => {
                                const item = (entry as { payload?: ComparacaoItem })?.payload;
                                return [`${formatSignedCurrency(Number(value))} (${formatSignedPercent(item?.deltaPercent ?? null)})`, 'Variação'];
                            }}
                        />
                        <Bar dataKey="delta" maxBarSize={20}>
                            {itens.map(item => (
                                <Cell key={item.categoryId} fill={item.delta <= 0 ? COR_QUEDA : COR_ALTA} />
                            ))}
                            <LabelList
                                dataKey="delta"
                                content={(props: LabelContentProps) => {
                                    const item = typeof props.index === 'number' ? itens[props.index] : undefined;
                                    if (!item) return null;
                                    const x = Number(props.x ?? 0) + Number(props.width ?? 0) + (item.delta >= 0 ? 6 : -6);
                                    const y = Number(props.y ?? 0) + Number(props.height ?? 0) / 2;
                                    return (
                                        <text x={x} y={y} dy={4} fontSize={11} fill="#64748b" textAnchor={item.delta >= 0 ? 'start' : 'end'}>
                                            {rotuloDelta(item)}
                                        </text>
                                    );
                                }}
                            />
                        </Bar>
                    </BarChart>
                </ResponsiveContainer>
            </ChartFigure>
        </div>
    );
}
