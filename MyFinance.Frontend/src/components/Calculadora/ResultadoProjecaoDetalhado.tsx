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
import type { ProjecaoInvestimentoResponseDto } from '../../types/ProjecaoInvestimento';
import { CategoriaTributariaAtivo } from '../../types/CategoriaTributariaAtivo';
import { formatCurrency } from './calculadoraUtils';

interface ResultadoProjecaoDetalhadoProps {
    resultado: ProjecaoInvestimentoResponseDto;
    /** Usado só para decidir o formato dos ticks do eixo X do gráfico (meses vs. anos). */
    prazoMeses: number;
}

/**
 * Bloco de exibição de um resultado de projeção: estatísticas principais,
 * detalhamento de tributos e gráfico de evolução. Compartilhado entre o modo
 * "Cenário único" e a "Meta reversa" — ambos produzem um único
 * `ProjecaoInvestimentoResponseDto` a exibir da mesma forma.
 */
export function ResultadoProjecaoDetalhado({ resultado, prazoMeses }: ResultadoProjecaoDetalhadoProps) {
    const chartTickFormatter = (mes: number) =>
        prazoMeses > 24 ? `${Math.round(mes / 12)}a` : `${mes}m`;

    return (
        <div className="proj-result">
            <div className="proj-result-stats">
                <div className="proj-result-stat proj-result-stat--highlight">
                    <span className="proj-result-stat-value">{formatCurrency(resultado.valorFinal)}</span>
                    <span className="proj-result-stat-label">Valor final projetado</span>
                </div>
                <div className="proj-result-stat">
                    <span className="proj-result-stat-value">{formatCurrency(resultado.totalAportado)}</span>
                    <span className="proj-result-stat-label">Total aportado</span>
                </div>
                <div className="proj-result-stat">
                    <span className="proj-result-stat-value proj-result-stat-value--green">
                        {formatCurrency(resultado.totalJuros)}
                    </span>
                    <span className="proj-result-stat-label">Total em juros</span>
                </div>
                <div className="proj-result-stat">
                    <span className="proj-result-stat-value">
                        {resultado.rentabilidadePercentual.toFixed(2)}%
                    </span>
                    <span className="proj-result-stat-label">
                        Rentabilidade (
                        {resultado.taxaJurosAnualUtilizada.toFixed(2)}% a.a.
                        {resultado.percentualCdiUtilizado != null && resultado.cdiAnualUtilizado != null
                            ? ` — ${resultado.percentualCdiUtilizado.toFixed(0)}% do CDI de ${resultado.cdiAnualUtilizado.toFixed(2)}%`
                            : ''}
                        )
                    </span>
                </div>
                {resultado.rentabilidadeRealAnualPercentual != null && (
                    <div className="proj-result-stat">
                        <span className="proj-result-stat-value">
                            {resultado.rentabilidadeRealAnualPercentual.toFixed(2)}%
                        </span>
                        <span className="proj-result-stat-label">
                            Rentabilidade real (IPCA {resultado.ipcaAnualUtilizado?.toFixed(2)}% a.a.)
                        </span>
                    </div>
                )}
            </div>

            {(resultado.valorIof > 0 || resultado.valorImpostoRenda > 0 || resultado.valorComeCotasRetido > 0) && (
                <div className="proj-tributos">
                    <h3 className="proj-tributos-title">Detalhamento de tributos</h3>
                    <div className="proj-tributos-row">
                        <span className="proj-tributos-label">Rendimento bruto</span>
                        <span className="proj-tributos-value">{formatCurrency(resultado.totalJuros)}</span>
                    </div>
                    {resultado.valorIof > 0 && (
                        <div className="proj-tributos-row">
                            <span className="proj-tributos-label">
                                IOF regressivo ({resultado.aliquotaIofPercentual.toFixed(1)}%)
                            </span>
                            <span className="proj-tributos-value proj-tributos-value--red">
                                -{formatCurrency(resultado.valorIof)}
                            </span>
                        </div>
                    )}
                    {resultado.valorComeCotasRetido > 0 && (
                        <div className="proj-tributos-row">
                            <span className="proj-tributos-label">
                                Come-cotas retido ({resultado.aliquotaComeCotasPercentual.toFixed(1)}% antecipado)
                            </span>
                            <span className="proj-tributos-value proj-tributos-value--red">
                                -{formatCurrency(resultado.valorComeCotasRetido)}
                            </span>
                        </div>
                    )}
                    {resultado.valorImpostoRenda > 0 && (
                        <div className="proj-tributos-row">
                            <span className="proj-tributos-label">
                                {resultado.valorComeCotasRetido > 0
                                    ? `Imposto de Renda complementar (regressivo, ${resultado.aliquotaImpostoRendaPercentual.toFixed(1)}%)`
                                    : resultado.categoriaTributaria === CategoriaTributariaAtivo.RendaFixaTributavel
                                        ? `Imposto de Renda (regressivo, ${resultado.aliquotaImpostoRendaPercentual.toFixed(1)}%)`
                                        : resultado.categoriaTributaria === CategoriaTributariaAtivo.PrevidenciaPgbl ||
                                            resultado.categoriaTributaria === CategoriaTributariaAtivo.PrevidenciaVgbl
                                            ? `Imposto de Renda (regressivo previdência, ${resultado.aliquotaImpostoRendaPercentual.toFixed(1)}%)`
                                            : `Imposto de Renda (ganho de capital, ${resultado.aliquotaImpostoRendaPercentual.toFixed(1)}%)`}
                            </span>
                            <span className="proj-tributos-value proj-tributos-value--red">
                                -{formatCurrency(resultado.valorImpostoRenda)}
                            </span>
                        </div>
                    )}
                    <div className="proj-tributos-row proj-tributos-row--total">
                        <span className="proj-tributos-label">Total de tributos</span>
                        <span className="proj-tributos-value proj-tributos-value--red">
                            -{formatCurrency(resultado.valorIof + resultado.valorComeCotasRetido + resultado.valorImpostoRenda)}
                        </span>
                    </div>
                    <div className="proj-tributos-row proj-tributos-row--highlight">
                        <span className="proj-tributos-label">Valor final líquido</span>
                        <span className="proj-tributos-value">{formatCurrency(resultado.valorFinalLiquido)}</span>
                    </div>
                </div>
            )}

            {resultado.isentoPorFaixaDeVenda && (
                <p className="proj-hint proj-isento-venda">
                    Isento de IR — o valor final simulado ficou abaixo do limite de isenção de{' '}
                    {resultado.categoriaTributaria === CategoriaTributariaAtivo.GanhoCapitalCripto
                        ? 'R$ 35.000 (criptomoedas)'
                        : 'R$ 20.000 (ações)'}
                    .
                </p>
            )}

            <div className="proj-chart">
                <ResponsiveContainer width="100%" height={320}>
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
                            tickFormatter={(v: number) => formatCurrency(v)}
                            width={90}
                            stroke="#94a3b8"
                            fontSize={12}
                        />
                        <Tooltip
                            formatter={(value, name) => [formatCurrency(Number(value)), name]}
                            labelFormatter={(mes) => `Mês ${mes}`}
                        />
                        <Legend />
                        <Area
                            type="monotone"
                            dataKey="valorAcumulado"
                            name="Valor acumulado"
                            stroke="#3b82f6"
                            strokeWidth={2}
                            fill="#3b82f6"
                            fillOpacity={0.15}
                        />
                        <Line
                            type="monotone"
                            dataKey="totalAportadoAcumulado"
                            name="Total aportado"
                            stroke="#94a3b8"
                            strokeWidth={2}
                            strokeDasharray="4 4"
                            dot={false}
                        />
                    </ComposedChart>
                </ResponsiveContainer>
            </div>
        </div>
    );
}
