import { useState } from 'react';
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
import { projecaoInvestimentoService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { ProjecaoInvestimentoResponseDto } from '../../types/ProjecaoInvestimento';
import './CalculadoraProjecao.css';

type PrazoUnidade = 'anos' | 'meses';
type TaxaModo = 'selic' | 'manual';

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

/** Máscara monetária: converte dígitos digitados em "1.234,56". */
function maskCurrency(raw: string): string {
    let digits = raw.replace(/\D/g, '');
    if (digits === '') return '';
    if (digits.length > 1) digits = digits.replace(/^0+/, '');
    while (digits.length < 3) digits = '0' + digits;
    const decimalIndex = digits.length - 2;
    const integerPart = digits.slice(0, decimalIndex);
    const decimalPart = digits.slice(decimalIndex);
    const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    return formattedInteger + ',' + decimalPart;
}

const parseCurrency = (masked: string): number => {
    const parsed = parseFloat(masked.replace(/\./g, '').replace(',', '.'));
    return isNaN(parsed) ? 0 : parsed;
};

const parsePercent = (raw: string): number | null => {
    const parsed = parseFloat(raw.replace(',', '.'));
    return isNaN(parsed) ? null : parsed;
};

export function CalculadoraProjecao() {
    const [aporteInicial, setAporteInicial] = useState('');
    const [aporteMensal, setAporteMensal] = useState('');
    const [prazoValor, setPrazoValor] = useState('10');
    const [prazoUnidade, setPrazoUnidade] = useState<PrazoUnidade>('anos');
    const [taxaModo, setTaxaModo] = useState<TaxaModo>('selic');
    const [taxaManual, setTaxaManual] = useState('');

    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [resultado, setResultado] = useState<ProjecaoInvestimentoResponseDto | null>(null);

    const prazoMeses = prazoUnidade === 'anos'
        ? Math.round(parseFloat(prazoValor || '0') * 12)
        : Math.round(parseFloat(prazoValor || '0'));

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (!prazoMeses || prazoMeses <= 0) {
            setError('Informe um prazo válido maior que zero.');
            return;
        }

        const taxaManualNumero = taxaModo === 'manual' ? parsePercent(taxaManual) : null;
        if (taxaModo === 'manual' && (taxaManualNumero === null || taxaManualNumero < 0)) {
            setError('Informe uma taxa de juros anual válida.');
            return;
        }

        setIsLoading(true);
        try {
            const data = await projecaoInvestimentoService.calcular({
                aporteInicial: parseCurrency(aporteInicial),
                aporteMensal: parseCurrency(aporteMensal),
                prazoMeses,
                usarTaxaSelic: taxaModo === 'selic',
                taxaJurosAnualPercentual: taxaModo === 'manual' ? taxaManualNumero! : undefined,
            });
            setResultado(data);
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Não foi possível calcular a projeção. Tente novamente.');
            setResultado(null);
        } finally {
            setIsLoading(false);
        }
    };

    const chartTickFormatter = (mes: number) =>
        prazoMeses > 24 ? `${Math.round(mes / 12)}a` : `${mes}m`;

    return (
        <div className="proj-container">
            <form className="proj-form" onSubmit={handleSubmit}>
                <div className="proj-form-row">
                    <div className="proj-form-group">
                        <label htmlFor="projAporteInicial">Aporte inicial (R$)</label>
                        <input
                            id="projAporteInicial"
                            type="text"
                            inputMode="numeric"
                            placeholder="0,00"
                            value={aporteInicial}
                            onChange={e => setAporteInicial(maskCurrency(e.target.value))}
                            disabled={isLoading}
                        />
                    </div>
                    <div className="proj-form-group">
                        <label htmlFor="projAporteMensal">Aporte mensal (R$)</label>
                        <input
                            id="projAporteMensal"
                            type="text"
                            inputMode="numeric"
                            placeholder="0,00"
                            value={aporteMensal}
                            onChange={e => setAporteMensal(maskCurrency(e.target.value))}
                            disabled={isLoading}
                        />
                    </div>
                </div>

                <div className="proj-form-group">
                    <label htmlFor="projPrazo">Prazo</label>
                    <div className="proj-prazo-row">
                        <input
                            id="projPrazo"
                            type="number"
                            min={1}
                            value={prazoValor}
                            onChange={e => setPrazoValor(e.target.value)}
                            disabled={isLoading}
                        />
                        <div className="proj-toggle-group" role="radiogroup" aria-label="Unidade do prazo">
                            <button
                                type="button"
                                role="radio"
                                aria-checked={prazoUnidade === 'anos'}
                                className={`proj-toggle-btn${prazoUnidade === 'anos' ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => setPrazoUnidade('anos')}
                                disabled={isLoading}
                            >
                                Anos
                            </button>
                            <button
                                type="button"
                                role="radio"
                                aria-checked={prazoUnidade === 'meses'}
                                className={`proj-toggle-btn${prazoUnidade === 'meses' ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => setPrazoUnidade('meses')}
                                disabled={isLoading}
                            >
                                Meses
                            </button>
                        </div>
                    </div>
                </div>

                <div className="proj-form-group">
                    <label>Taxa de juros</label>
                    <div className="proj-toggle-group proj-toggle-group--full" role="radiogroup" aria-label="Fonte da taxa de juros">
                        <button
                            type="button"
                            role="radio"
                            aria-checked={taxaModo === 'selic'}
                            className={`proj-toggle-btn${taxaModo === 'selic' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setTaxaModo('selic')}
                            disabled={isLoading}
                        >
                            Tesouro Direto (Selic atual)
                        </button>
                        <button
                            type="button"
                            role="radio"
                            aria-checked={taxaModo === 'manual'}
                            className={`proj-toggle-btn${taxaModo === 'manual' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setTaxaModo('manual')}
                            disabled={isLoading}
                        >
                            Taxa manual
                        </button>
                    </div>
                    {taxaModo === 'manual' && (
                        <input
                            className="proj-taxa-manual-input"
                            type="text"
                            inputMode="decimal"
                            placeholder="Ex: 10,5 (% ao ano)"
                            value={taxaManual}
                            onChange={e => setTaxaManual(e.target.value)}
                            disabled={isLoading}
                        />
                    )}
                    {taxaModo === 'selic' && (
                        <p className="proj-hint">
                            A taxa Selic real vigente será buscada automaticamente ao calcular.
                        </p>
                    )}
                </div>

                {error && <span className="proj-error">{error}</span>}

                <button type="submit" className="proj-btn-submit" disabled={isLoading}>
                    {isLoading ? 'Calculando...' : 'Calcular projeção'}
                </button>
            </form>

            {resultado && (
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
                                Rentabilidade ({resultado.taxaJurosAnualUtilizada.toFixed(2)}% a.a.)
                            </span>
                        </div>
                    </div>

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
            )}
        </div>
    );
}
