import {
    ComposedChart,
    Area,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    Legend,
    ResponsiveContainer,
} from 'recharts';
import type { RetiradaResponseDto } from '../../types/Retirada';
import { formatCurrency } from './calculadoraUtils';
import { formatPrazo } from './calculadoraValidacao';
import { yAxisProps, formatCurrencyCompacta } from '../Shared/charts/chartTheme';
import { useIsMobile } from '../../hooks/useIsMobile';

interface ResultadoRetiradaDetalhadoProps {
    resultado: RetiradaResponseDto;
}

export function ResultadoRetiradaDetalhado({ resultado }: ResultadoRetiradaDetalhadoProps) {
    const ehMobile = useIsMobile();
    const totalImposto = resultado.evolucao.reduce((soma, m) => soma + m.valorImposto, 0);
    const totalSaqueBruto = resultado.evolucao.reduce((soma, m) => soma + m.saqueBruto, 0);
    const aliquotaMedia = totalSaqueBruto > 0 ? (totalImposto / totalSaqueBruto) * 100 : 0;

    const chartTickFormatter = (mes: number) =>
        resultado.evolucao.length > 24 ? `${Math.round(mes / 12)}a` : `${mes}m`;

    return (
        <div className="proj-result">
            <div className="proj-result-stats">
                <div className="proj-result-stat proj-result-stat--highlight">
                    <span className="proj-result-stat-value">{formatCurrency(resultado.saqueMensal)}</span>
                    <span className="proj-result-stat-label">Saque mensal bruto</span>
                </div>
                <div className="proj-result-stat">
                    <span className="proj-result-stat-value">
                        {resultado.duraParaSempre
                            ? 'Para sempre'
                            : formatPrazo(resultado.mesEsgotamento ?? resultado.evolucao.length)}
                    </span>
                    <span className="proj-result-stat-label">
                        {resultado.duraParaSempre ? 'Duração do saldo' : 'Duração até esgotar'}
                    </span>
                </div>
                <div className="proj-result-stat">
                    <span className="proj-result-stat-value proj-result-stat-value--green">
                        {formatCurrency(resultado.evolucao[0]?.saqueLiquido ?? 0)}
                    </span>
                    <span className="proj-result-stat-label">Saque líquido no 1º mês</span>
                </div>
                <div className="proj-result-stat">
                    <span className="proj-result-stat-value">{aliquotaMedia.toFixed(1)}%</span>
                    <span className="proj-result-stat-label">Alíquota média de IR sobre os saques</span>
                </div>
            </div>

            <div className="proj-tributos">
                <h3 className="proj-tributos-title">Detalhamento de tributos (acumulado no período exibido)</h3>
                <div className="proj-tributos-row">
                    <span className="proj-tributos-label">Total sacado (bruto)</span>
                    <span className="proj-tributos-value">{formatCurrency(totalSaqueBruto)}</span>
                </div>
                <div className="proj-tributos-row">
                    <span className="proj-tributos-label">Imposto de Renda retido</span>
                    <span className="proj-tributos-value proj-tributos-value--red">-{formatCurrency(totalImposto)}</span>
                </div>
                <div className="proj-tributos-row proj-tributos-row--highlight">
                    <span className="proj-tributos-label">Total recebido líquido</span>
                    <span className="proj-tributos-value">{formatCurrency(totalSaqueBruto - totalImposto)}</span>
                </div>
            </div>

            <div className="proj-chart">
                <ResponsiveContainer width="100%" height={ehMobile ? 260 : 320}>
                    <ComposedChart data={resultado.evolucao} margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
                        <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                        <XAxis
                            dataKey="mes"
                            tickFormatter={chartTickFormatter}
                            interval={Math.max(0, Math.ceil(resultado.evolucao.length / 10) - 1)}
                            stroke="#94a3b8"
                            fontSize={12}
                        />
                        <YAxis
                            tickFormatter={(v: number) => ehMobile ? formatCurrencyCompacta(v) : formatCurrency(v)}
                            {...yAxisProps(ehMobile)}
                        />
                        <Tooltip
                            formatter={(value, name) => [formatCurrency(Number(value)), name]}
                            labelFormatter={(mes) => `Mês ${mes}`}
                        />
                        <Legend />
                        <Area
                            type="monotone"
                            dataKey="saldoFinal"
                            name="Saldo restante"
                            stroke="#3b82f6"
                            strokeWidth={2}
                            fill="#3b82f6"
                            fillOpacity={0.15}
                        />
                        <Line
                            type="monotone"
                            dataKey="saqueLiquido"
                            name="Saque líquido"
                            stroke="#10b981"
                            strokeWidth={2}
                            dot={false}
                        />
                    </ComposedChart>
                </ResponsiveContainer>
            </div>
        </div>
    );
}
