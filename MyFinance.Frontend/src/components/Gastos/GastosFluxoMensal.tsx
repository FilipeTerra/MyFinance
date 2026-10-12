import { ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid, ReferenceLine, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ExpenseTimelineResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency, formatMesLabel, formatMesLabelCompleto } from './gastosUtils';
import {
    intervaloEixoX,
    COR_RECEITA,
    COR_DESPESA,
    COR_SALDO,
    CHART_MARGIN,
    gridProps,
    xAxisProps,
    yAxisProps,
    BAR_RADIUS,
    MAX_BAR_SIZE,
    formatCurrencyCompacta,
} from '../Shared/charts/chartTheme';
import { ChartFigure } from '../Shared/charts/ChartFigure';
import { EstadoVazio } from '../Shared/ui';
import { useIsMobile } from '../../hooks/useIsMobile';

interface GastosFluxoMensalProps {
    timeline: ExpenseTimelineResponseDto;
}

/** Cinza do eixo zero — `--slate-300`, repetido aqui porque o Recharts recebe prop, não CSS. */
const COR_ZERO = '#cbd5e1';

/**
 * Fluxo mensal de receitas x despesas com linha de saldo. Extraído do card
 * único que também continha a lista de maiores lançamentos (agora em
 * `GastosMaioresLancamentos`) — este componente é só o gráfico, montado
 * dentro do card do seletor de visão em `GastosEvolucao`.
 */
export function GastosFluxoMensal({ timeline }: GastosFluxoMensalProps) {
    const ehMobile = useIsMobile();
    const altura = ehMobile ? 220 : 280;

    const dados = timeline.months.map(mes => {
        const semMovimentacao = mes.totalIncome === 0 && mes.totalExpenses === 0;
        return {
            label: mes.label,
            mesCompleto: formatMesLabelCompleto(mes.label),
            receita: mes.totalIncome,
            despesa: mes.totalExpenses,
            // Mês sem movimentação não tem saldo zero: não tem saldo. Plotá-lo
            // como 0 fazia a linha mergulhar até o eixo e voltar, desenhando um
            // vale que não corresponde a nenhuma medição. Como `null`, a linha
            // simplesmente se interrompe ali.
            saldo: semMovimentacao ? null : mes.balance,
        };
    });

    const semDados = dados.every(d => d.receita === 0 && d.despesa === 0);
    if (semDados) {
        return <EstadoVazio variante="inline" icone="💸" titulo="Nenhuma movimentação encontrada no período" />;
    }

    return (
        <ChartFigure
            titulo="Fluxo mensal"
            descricao="Receitas, despesas e saldo por mês no período selecionado"
            altura={altura}
            dadosTabela={{
                colunas: ['Mês', 'Receitas', 'Despesas', 'Saldo'],
                linhas: dados.map(d => [
                    d.mesCompleto,
                    formatCurrency(d.receita),
                    formatCurrency(d.despesa),
                    d.saldo === null ? '—' : formatCurrency(d.saldo),
                ]),
            }}
        >
            <ResponsiveContainer width="100%" height={altura}>
                <ComposedChart data={dados} margin={CHART_MARGIN}>
                    <CartesianGrid {...gridProps} />
                    <XAxis dataKey="label" tickFormatter={formatMesLabel} interval={intervaloEixoX(dados.length)} {...xAxisProps} />
                    <YAxis tickFormatter={(v: number) => ehMobile ? formatCurrencyCompacta(v) : formatCurrency(v)} {...yAxisProps(ehMobile)} />
                    <Tooltip
                        formatter={(value, name) => [formatCurrency(Number(value)), name]}
                        labelFormatter={(_label, payload) => (payload?.[0]?.payload as { mesCompleto?: string })?.mesCompleto ?? ''}
                    />
                    <Legend />
                    {/* Sem esta referência não há como ler o sinal do saldo quando ele fica negativo. */}
                    <ReferenceLine y={0} stroke={COR_ZERO} />
                    <Bar dataKey="receita" name="Receitas" fill={COR_RECEITA} maxBarSize={MAX_BAR_SIZE} radius={BAR_RADIUS} />
                    <Bar dataKey="despesa" name="Despesas" fill={COR_DESPESA} maxBarSize={MAX_BAR_SIZE} radius={BAR_RADIUS} />
                    {/* `linear` e não `monotone`: valores mensais são observações discretas e a
                        spline inventava curvatura entre elas. Traço sólido com ponto em cada mês —
                        tracejado, em gráfico financeiro, convenciona projeção ou meta, e saldo é
                        dado medido. `connectNulls` explícito para a linha interromper no mês sem
                        movimentação em vez de atravessá-lo. */}
                    <Line
                        type="linear"
                        dataKey="saldo"
                        name="Saldo"
                        stroke={COR_SALDO}
                        strokeWidth={2}
                        connectNulls={false}
                        dot={{ r: 3, fill: COR_SALDO, strokeWidth: 0 }}
                        activeDot={{ r: 5 }}
                    />
                </ComposedChart>
            </ResponsiveContainer>
        </ChartFigure>
    );
}
