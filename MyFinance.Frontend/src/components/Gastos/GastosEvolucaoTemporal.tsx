import { useState } from 'react';
import { BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ExpenseTimelineResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency, formatMesLabel, formatMesLabelCompleto } from './gastosUtils';
import { corDaCategoria, type RankingCategorias } from './gastosSelectors';
import { intervaloEixoX, COR_NEUTRA } from '../Shared/charts/chartTheme';
import { ChartFigure } from '../Shared/charts/ChartFigure';
import { EstadoVazio } from '../Shared/ui';
import './GastosEvolucaoTemporal.css';

interface GastosEvolucaoTemporalProps {
    timeline: ExpenseTimelineResponseDto;
    /** Ranking único da aba (construído em AnaliseGastos) — antes este componente calculava seu próprio top-N a partir de uma fonte diferente do donut. */
    ranking: RankingCategorias;
    visao: 'composicao' | 'tendencia';
}

const MAX_LINHAS_SELECIONADAS = 4;

interface SerieDisponivel {
    categoryId: string;
    categoryName: string;
}

function construirDadosMensais(months: ExpenseTimelineResponseDto['months'], principais: SerieDisponivel[], temOutras: boolean) {
    return months.map(mes => {
        const linha: Record<string, number | string> = { label: mes.label, mesCompleto: formatMesLabelCompleto(mes.label) };
        principais.forEach(p => { linha[p.categoryId] = 0; });
        if (temOutras) linha.__outras__ = 0;

        let somaOutras = 0;
        mes.categories.forEach(cat => {
            if (principais.some(p => p.categoryId === cat.categoryId)) linha[cat.categoryId] = cat.total;
            else somaOutras += cat.total;
        });
        if (temOutras) linha.__outras__ = somaOutras;

        return linha;
    });
}

/**
 * Evolução mensal por categoria — barras empilhadas (composição de cada mês)
 * ou linhas isoladas (tendência de até 4 categorias). O modo em si agora é
 * escolhido pelo seletor de visão em `GastosEvolucao`; este componente só
 * desenha o gráfico da visão ativa.
 */
export function GastosEvolucaoTemporal({ timeline, ranking, visao }: GastosEvolucaoTemporalProps) {
    const { principais, temOutras } = ranking;
    const dados = construirDadosMensais(timeline.months, principais, temOutras);

    const seriesDisponiveis: SerieDisponivel[] = temOutras
        ? [...principais, { categoryId: '__outras__', categoryName: 'Outras' }]
        : principais;

    // Estado "bruto" do que o usuário clicou — pode ficar órfão quando o
    // período/conta muda e algum ID selecionado deixa de existir. Em vez de
    // sincronizar com um useEffect (que causaria um frame com o gráfico
    // vazio), a seleção EFETIVA é sempre derivada na renderização: filtra
    // pelos IDs disponíveis agora e, se sobrar nada, cai para os 3 primeiros.
    const [selecaoBruta, setSelecaoBruta] = useState<string[] | null>(null);
    const idsDisponiveis = new Set(seriesDisponiveis.map(s => s.categoryId));
    const selecaoValida = (selecaoBruta ?? []).filter(id => idsDisponiveis.has(id));
    const categoriasSelecionadas = selecaoValida.length > 0 ? selecaoValida : seriesDisponiveis.slice(0, 3).map(s => s.categoryId);

    const toggleCategoria = (categoryId: string) => {
        setSelecaoBruta(prev => {
            const atual = prev ?? categoriasSelecionadas;
            if (atual.includes(categoryId)) return atual.filter(id => id !== categoryId);
            if (atual.length >= MAX_LINHAS_SELECIONADAS) return atual;
            return [...atual, categoryId];
        });
    };

    const corDaSerie = (categoryId: string) => (categoryId === '__outras__' ? COR_NEUTRA : corDaCategoria(ranking, categoryId));

    if (timeline.months.every(mes => mes.totalExpenses === 0)) {
        return <EstadoVazio variante="inline" icone="📊" titulo="Nenhuma despesa encontrada no período" />;
    }

    const xAxisInterval = intervaloEixoX(dados.length);
    const seriesTabela = visao === 'composicao'
        ? seriesDisponiveis
        : seriesDisponiveis.filter(s => categoriasSelecionadas.includes(s.categoryId));

    return (
        <div className="evolucao-conteudo">
            {visao === 'tendencia' && (
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
                                <span className="evolucao-chip-dot" style={{ background: corDaSerie(categoria.categoryId) }} aria-hidden="true" />
                                {categoria.categoryName}
                            </button>
                        );
                    })}
                </div>
            )}

            <ChartFigure
                titulo={visao === 'composicao' ? 'Composição mensal por categoria' : 'Tendência por categoria'}
                descricao={visao === 'composicao'
                    ? 'Barras empilhadas com o total de despesas de cada categoria, mês a mês'
                    : 'Linhas de tendência das categorias selecionadas, mês a mês'}
                altura={300}
                dadosTabela={{
                    colunas: ['Mês', ...seriesTabela.map(s => s.categoryName)],
                    linhas: dados.map(d => [
                        String(d.mesCompleto),
                        ...seriesTabela.map(s => formatCurrency(Number(d[s.categoryId] ?? 0))),
                    ]),
                }}
            >
                <ResponsiveContainer width="100%" height={300}>
                    {visao === 'composicao' ? (
                        <BarChart data={dados} margin={{ top: 8, right: 16, left: 8, bottom: 0 }} barCategoryGap="24%">
                            <CartesianGrid stroke="#e2e8f0" vertical={false} />
                            <XAxis dataKey="label" tickFormatter={formatMesLabel} interval={xAxisInterval} stroke="#94a3b8" fontSize={12} tick={{ fill: '#64748b' }} />
                            <YAxis tickFormatter={(v: number) => formatCurrency(v)} width={90} stroke="#94a3b8" fontSize={12} tick={{ fill: '#64748b' }} />
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
                                    stroke="var(--color-surface)"
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
                                    fill={COR_NEUTRA}
                                    stroke="var(--color-surface)"
                                    strokeWidth={2}
                                    maxBarSize={24}
                                    radius={[4, 4, 0, 0]}
                                />
                            )}
                        </BarChart>
                    ) : (
                        <LineChart data={dados} margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
                            <CartesianGrid stroke="#e2e8f0" vertical={false} />
                            <XAxis dataKey="label" tickFormatter={formatMesLabel} interval={xAxisInterval} stroke="#94a3b8" fontSize={12} tick={{ fill: '#64748b' }} />
                            <YAxis tickFormatter={(v: number) => formatCurrency(v)} width={90} stroke="#94a3b8" fontSize={12} tick={{ fill: '#64748b' }} />
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
                                        dot={{ r: 4, strokeWidth: 2, stroke: 'var(--color-surface)' }}
                                    />
                                ))}
                        </LineChart>
                    )}
                </ResponsiveContainer>
            </ChartFigure>
        </div>
    );
}
