import { ComposedChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ExpenseTimelineResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency, formatMesLabel, formatMesLabelCompleto } from './gastosUtils';
import {
    intervaloEixoX,
    COR_RECEITA,
    COR_DESPESA,
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

/**
 * Fluxo mensal de receitas x despesas. Extraído do card único que também
 * continha a lista de maiores lançamentos (agora em
 * `GastosMaioresLancamentos`) — este componente é só o gráfico, montado
 * dentro do card do seletor de visão em `GastosEvolucao`.
 *
 * O saldo do mês não é plotado: como série sobre as barras ele não se
 * sustentava (ver histórico deste arquivo) e, sendo derivado de receita menos
 * despesa, as duas barras já contam a história. Continua disponível por mês na
 * tabela que o `ChartFigure` gera para leitor de tela.
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
            // Mês sem movimentação não tem saldo zero: não tem saldo. Só a
            // tabela acessível consome este campo, que ali vira "—".
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
            descricao="Receitas e despesas por mês no período selecionado"
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
                    <Bar dataKey="receita" name="Receitas" fill={COR_RECEITA} maxBarSize={MAX_BAR_SIZE} radius={BAR_RADIUS} />
                    <Bar dataKey="despesa" name="Despesas" fill={COR_DESPESA} maxBarSize={MAX_BAR_SIZE} radius={BAR_RADIUS} />
                </ComposedChart>
            </ResponsiveContainer>
        </ChartFigure>
    );
}
