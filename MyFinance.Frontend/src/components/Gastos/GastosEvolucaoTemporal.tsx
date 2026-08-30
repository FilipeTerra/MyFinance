import { useMemo, useState } from 'react';
import {
    BarChart,
    Bar,
    LineChart,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    Legend,
    ResponsiveContainer,
} from 'recharts';
import type { ExpenseTimelineResponseDto } from '../../types/ExpenseAnalytics';
import { corDaCategoria, formatCurrency, formatMesLabel, formatMesLabelCompleto, COR_OUTRAS } from './gastosUtils';
import './GastosEvolucaoTemporal.css';

interface GastosEvolucaoTemporalProps {
    timeline: ExpenseTimelineResponseDto;
    coresCategorias: Map<string, string>;
}

const MAX_CATEGORIAS_EMPILHADAS = 5;
const MAX_LINHAS_SELECIONADAS = 4;

interface CategoriaResumo {
    categoryId: string;
    categoryName: string;
    total: number;
}

/** Agrega o total de cada categoria em toda a janela, para decidir quais viram série própria. */
function ranquearCategorias(months: ExpenseTimelineResponseDto['months']): CategoriaResumo[] {
    const totais = new Map<string, CategoriaResumo>();
    months.forEach(mes => {
        mes.categories.forEach(cat => {
            const atual = totais.get(cat.categoryId);
            if (atual) atual.total += cat.total;
            else totais.set(cat.categoryId, { categoryId: cat.categoryId, categoryName: cat.categoryName, total: cat.total });
        });
    });
    return [...totais.values()].sort((a, b) => b.total - a.total);
}

/** Constrói uma linha por mês com uma coluna por categoria principal (+ "Outras" quando cabível). */
function construirDadosMensais(
    months: ExpenseTimelineResponseDto['months'],
    principais: CategoriaResumo[],
    temOutras: boolean,
) {
    return months.map(mes => {
        const linha: Record<string, number | string> = {
            label: mes.label,
            mesCompleto: formatMesLabelCompleto(mes.label),
        };
        principais.forEach(p => { linha[p.categoryId] = 0; });
        if (temOutras) linha.__outras__ = 0;

        let somaOutras = 0;
        mes.categories.forEach(cat => {
            if (principais.some(p => p.categoryId === cat.categoryId)) {
                linha[cat.categoryId] = cat.total;
            } else {
                somaOutras += cat.total;
            }
        });
        if (temOutras) linha.__outras__ = somaOutras;

        return linha;
    });
}

/**
 * Evolução mensal dos gastos por categoria: barras empilhadas (composição do mês) com opção de
 * alternar para linhas isoladas por categoria (tendência individual, limitada a 4 séries — acima
 * disso linhas convergentes deixam de ser legíveis).
 */
export function GastosEvolucaoTemporal({ timeline, coresCategorias }: GastosEvolucaoTemporalProps) {
    const [modo, setModo] = useState<'empilhado' | 'linhas'>('empilhado');

    const { principais, temOutras, dados } = useMemo(() => {
        const ranking = ranquearCategorias(timeline.months);
        const principaisCalc = ranking.slice(0, MAX_CATEGORIAS_EMPILHADAS);
        const temOutrasCalc = ranking.length > MAX_CATEGORIAS_EMPILHADAS;
        return {
            principais: principaisCalc,
            temOutras: temOutrasCalc,
            dados: construirDadosMensais(timeline.months, principaisCalc, temOutrasCalc),
        };
    }, [timeline]);

    const [categoriasSelecionadas, setCategoriasSelecionadas] = useState<string[]>(
        () => principais.slice(0, 3).map(c => c.categoryId),
    );

    const seriesDisponiveis = temOutras
        ? [...principais, { categoryId: '__outras__', categoryName: 'Outras', total: 0 }]
        : principais;

    const toggleCategoria = (categoryId: string) => {
        setCategoriasSelecionadas(prev => {
            if (prev.includes(categoryId)) return prev.filter(id => id !== categoryId);
            if (prev.length >= MAX_LINHAS_SELECIONADAS) return prev;
            return [...prev, categoryId];
        });
    };

    const corDaSerie = (categoryId: string) => (categoryId === '__outras__' ? COR_OUTRAS : corDaCategoria(coresCategorias, categoryId));

    if (timeline.months.every(mes => mes.totalExpenses === 0)) {
        return (
            <div className="gastos-card">
                <h3 className="gastos-card-title">Evolução temporal por categoria</h3>
                <p className="gastos-card-empty">Nenhuma despesa encontrada no período selecionado.</p>
            </div>
        );
    }

    const xAxisInterval = Math.max(0, Math.ceil(dados.length / 10) - 1);

    return (
        <div className="gastos-card">
            <div className="evolucao-header">
                <h3 className="gastos-card-title">Evolução temporal por categoria</h3>
                <div className="gastos-toggle-group" role="radiogroup" aria-label="Modo de exibição">
                    <button
                        type="button"
                        role="radio"
                        aria-checked={modo === 'empilhado'}
                        className={`gastos-toggle-btn${modo === 'empilhado' ? ' gastos-toggle-btn--active' : ''}`}
                        onClick={() => setModo('empilhado')}
                    >
                        Empilhado
                    </button>
                    <button
                        type="button"
                        role="radio"
                        aria-checked={modo === 'linhas'}
                        className={`gastos-toggle-btn${modo === 'linhas' ? ' gastos-toggle-btn--active' : ''}`}
                        onClick={() => setModo('linhas')}
                    >
                        Linhas por categoria
                    </button>
                </div>
            </div>

            {modo === 'linhas' && (
                <div className="evolucao-chips" role="group" aria-label="Categorias exibidas (máx. 4)">
                    {seriesDisponiveis.map(categoria => {
                        const selecionada = categoriasSelecionadas.includes(categoria.categoryId);
                        const noLimite = !selecionada && categoriasSelecionadas.length >= MAX_LINHAS_SELECIONADAS;
                        return (
                            <button
                                key={categoria.categoryId}
                                type="button"
                                className={`evolucao-chip${selecionada ? ' evolucao-chip--active' : ''}`}
                                style={selecionada ? { borderColor: corDaSerie(categoria.categoryId) } : undefined}
                                onClick={() => toggleCategoria(categoria.categoryId)}
                                disabled={noLimite}
                                aria-pressed={selecionada}
                            >
                                <span
                                    className="evolucao-chip-dot"
                                    style={{ background: corDaSerie(categoria.categoryId) }}
                                    aria-hidden="true"
                                />
                                {categoria.categoryName}
                            </button>
                        );
                    })}
                </div>
            )}

            <ResponsiveContainer width="100%" height={300}>
                {modo === 'empilhado' ? (
                    <BarChart data={dados} margin={{ top: 8, right: 16, left: 8, bottom: 0 }} barCategoryGap="24%">
                        <CartesianGrid stroke="#e2e8f0" vertical={false} />
                        <XAxis dataKey="label" tickFormatter={formatMesLabel} interval={xAxisInterval} stroke="#94a3b8" fontSize={12} />
                        <YAxis tickFormatter={(v: number) => formatCurrency(v)} width={90} stroke="#94a3b8" fontSize={12} />
                        <Tooltip
                            formatter={(value, name) => [formatCurrency(Number(value)), name]}
                            labelFormatter={(_label, payload) => (payload?.[0]?.payload as { mesCompleto?: string })?.mesCompleto ?? ''}
                        />
                        <Legend />
                        {principais.map((categoria, index) => (
                            <Bar
                                key={categoria.categoryId}
                                dataKey={categoria.categoryId}
                                name={categoria.categoryName}
                                stackId="gastos"
                                fill={corDaSerie(categoria.categoryId)}
                                stroke="#ffffff"
                                strokeWidth={2}
                                maxBarSize={24}
                                radius={!temOutras && index === principais.length - 1 ? [4, 4, 0, 0] : undefined}
                            />
                        ))}
                        {temOutras && (
                            <Bar
                                dataKey="__outras__"
                                name="Outras"
                                stackId="gastos"
                                fill={COR_OUTRAS}
                                stroke="#ffffff"
                                strokeWidth={2}
                                maxBarSize={24}
                                radius={[4, 4, 0, 0]}
                            />
                        )}
                    </BarChart>
                ) : (
                    <LineChart data={dados} margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
                        <CartesianGrid stroke="#e2e8f0" vertical={false} />
                        <XAxis dataKey="label" tickFormatter={formatMesLabel} interval={xAxisInterval} stroke="#94a3b8" fontSize={12} />
                        <YAxis tickFormatter={(v: number) => formatCurrency(v)} width={90} stroke="#94a3b8" fontSize={12} />
                        <Tooltip
                            formatter={(value, name) => [formatCurrency(Number(value)), name]}
                            labelFormatter={(_label, payload) => (payload?.[0]?.payload as { mesCompleto?: string })?.mesCompleto ?? ''}
                        />
                        <Legend />
                        {seriesDisponiveis
                            .filter(categoria => categoriasSelecionadas.includes(categoria.categoryId))
                            .map(categoria => (
                                <Line
                                    key={categoria.categoryId}
                                    type="monotone"
                                    dataKey={categoria.categoryId}
                                    name={categoria.categoryName}
                                    stroke={corDaSerie(categoria.categoryId)}
                                    strokeWidth={2}
                                    dot={{ r: 4, strokeWidth: 2, stroke: '#ffffff' }}
                                />
                            ))}
                    </LineChart>
                )}
            </ResponsiveContainer>
        </div>
    );
}
