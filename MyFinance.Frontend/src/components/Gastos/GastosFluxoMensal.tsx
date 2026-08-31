import { ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ExpenseTimelineResponseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency, formatMesLabel, formatMesLabelCompleto } from './gastosUtils';
import { intervaloEixoX, COR_RECEITA, COR_DESPESA, COR_SALDO } from '../Shared/charts/chartTheme';
import { ChartFigure } from '../Shared/charts/ChartFigure';
import { EstadoVazio } from '../Shared/ui';

interface GastosFluxoMensalProps {
    timeline: ExpenseTimelineResponseDto;
}

/**
 * Fluxo mensal de receitas x despesas com linha de saldo. Extraído do card
 * único que também continha a lista de maiores lançamentos (agora em
 * `GastosMaioresLancamentos`) — este componente é só o gráfico, montado
 * dentro do card do seletor de visão em `GastosEvolucao`.
 */
export function GastosFluxoMensal({ timeline }: GastosFluxoMensalProps) {
    const dados = timeline.months.map(mes => ({
        label: mes.label,
        mesCompleto: formatMesLabelCompleto(mes.label),
        receita: mes.totalIncome,
        despesa: mes.totalExpenses,
        saldo: mes.balance,
    }));

    const semDados = dados.every(d => d.receita === 0 && d.despesa === 0);
    if (semDados) {
        return <EstadoVazio variante="inline" icone="💸" titulo="Nenhuma movimentação encontrada no período" />;
    }

    return (
        <ChartFigure
            titulo="Fluxo mensal"
            descricao="Receitas, despesas e saldo por mês no período selecionado"
            altura={280}
            dadosTabela={{
                colunas: ['Mês', 'Receitas', 'Despesas', 'Saldo'],
                linhas: dados.map(d => [d.mesCompleto, formatCurrency(d.receita), formatCurrency(d.despesa), formatCurrency(d.saldo)]),
            }}
        >
            <ResponsiveContainer width="100%" height={280}>
                <ComposedChart data={dados} margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
                    <CartesianGrid stroke="#e2e8f0" vertical={false} />
                    <XAxis dataKey="label" tickFormatter={formatMesLabel} interval={intervaloEixoX(dados.length)} stroke="#94a3b8" fontSize={12} tick={{ fill: '#64748b' }} />
                    <YAxis tickFormatter={(v: number) => formatCurrency(v)} width={90} stroke="#94a3b8" fontSize={12} tick={{ fill: '#64748b' }} />
                    <Tooltip
                        formatter={(value, name) => [formatCurrency(Number(value)), name]}
                        labelFormatter={(_label, payload) => (payload?.[0]?.payload as { mesCompleto?: string })?.mesCompleto ?? ''}
                    />
                    <Legend />
                    <Bar dataKey="receita" name="Receitas" fill={COR_RECEITA} maxBarSize={20} radius={[4, 4, 0, 0]} />
                    <Bar dataKey="despesa" name="Despesas" fill={COR_DESPESA} maxBarSize={20} radius={[4, 4, 0, 0]} />
                    <Line type="monotone" dataKey="saldo" name="Saldo" stroke={COR_SALDO} strokeWidth={2} strokeDasharray="4 4" dot={false} />
                </ComposedChart>
            </ResponsiveContainer>
        </ChartFigure>
    );
}
