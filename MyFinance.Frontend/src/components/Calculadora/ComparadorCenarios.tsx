import { useState } from 'react';
import {
    ComposedChart,
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
import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';
import { FonteTaxaJuros } from '../../types/FonteTaxaJuros';
import { GRUPOS_TIPO_ATIVO, GRUPO_TIPO_ATIVO_LABEL, TIPO_ATIVO_CALCULADORA_META, tiposAtivoPorGrupo } from './tipoAtivoCalculadoraMeta';
import { formatCurrency, maskCurrency, parseCurrency, parsePercent } from './calculadoraUtils';
import './ComparadorCenarios.css';

type PrazoUnidade = 'anos' | 'meses';
type TaxaModo = 'selic' | 'cdi' | 'manual';

/** Paleta categórica validada (ordem fixa — nunca reatribuída por rank). */
const CORES_CENARIO = ['#2a78d6', '#eb6834', '#1baf7a', '#eda100'];

const MIN_CENARIOS = 2;
const MAX_CENARIOS = 4;

interface CenarioConfig {
    id: string;
    tipoAtivo: TipoAtivoCalculadora;
    taxaModo: TaxaModo;
    taxaManual: string;
    percentualCdi: string;
}

let proximoId = 0;
const novoCenario = (tipoAtivo: TipoAtivoCalculadora, taxaModo: TaxaModo): CenarioConfig => ({
    id: `cenario-${proximoId++}`,
    tipoAtivo,
    taxaModo,
    taxaManual: '',
    percentualCdi: '100',
});

const cenariosIniciais = (): CenarioConfig[] => [
    novoCenario(TipoAtivoCalculadora.TesouroSelic, 'selic'),
    novoCenario(TipoAtivoCalculadora.Cdb, 'cdi'),
];

interface ResultadoCenario {
    cenario: CenarioConfig;
    resultado: ProjecaoInvestimentoResponseDto;
}

export function ComparadorCenarios() {
    const [aporteInicial, setAporteInicial] = useState('');
    const [aporteMensal, setAporteMensal] = useState('');
    const [prazoValor, setPrazoValor] = useState('10');
    const [prazoUnidade, setPrazoUnidade] = useState<PrazoUnidade>('anos');
    const [cenarios, setCenarios] = useState<CenarioConfig[]>(cenariosIniciais);

    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [resultados, setResultados] = useState<ResultadoCenario[] | null>(null);

    const prazoMeses = prazoUnidade === 'anos'
        ? Math.round(parseFloat(prazoValor || '0') * 12)
        : Math.round(parseFloat(prazoValor || '0'));

    const atualizarCenario = (id: string, patch: Partial<CenarioConfig>) =>
        setCenarios(prev => prev.map(c => (c.id === id ? { ...c, ...patch } : c)));

    const adicionarCenario = () => {
        if (cenarios.length >= MAX_CENARIOS) return;
        setCenarios(prev => [...prev, novoCenario(TipoAtivoCalculadora.Lci, 'manual')]);
    };

    const removerCenario = (id: string) => {
        if (cenarios.length <= MIN_CENARIOS) return;
        setCenarios(prev => prev.filter(c => c.id !== id));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (!prazoMeses || prazoMeses <= 0) {
            setError('Informe um prazo válido maior que zero.');
            return;
        }

        for (const cenario of cenarios) {
            if (cenario.taxaModo === 'manual') {
                const valor = parsePercent(cenario.taxaManual);
                if (valor === null || valor < 0) {
                    setError('Informe uma taxa de juros anual válida em todos os cenários manuais.');
                    return;
                }
            }
            if (cenario.taxaModo === 'cdi') {
                const valor = parsePercent(cenario.percentualCdi);
                if (valor === null || valor < 0) {
                    setError('Informe um percentual do CDI válido em todos os cenários "% do CDI".');
                    return;
                }
            }
        }

        const aporteInicialNumero = parseCurrency(aporteInicial);
        const aporteMensalNumero = parseCurrency(aporteMensal);

        setIsLoading(true);
        try {
            const respostas = await Promise.all(
                cenarios.map(cenario => {
                    const fonteTaxaJuros = cenario.taxaModo === 'selic'
                        ? FonteTaxaJuros.Selic
                        : cenario.taxaModo === 'cdi'
                            ? FonteTaxaJuros.PercentualCdi
                            : FonteTaxaJuros.Manual;

                    return projecaoInvestimentoService.calcular({
                        aporteInicial: aporteInicialNumero,
                        aporteMensal: aporteMensalNumero,
                        prazoMeses,
                        fonteTaxaJuros,
                        taxaJurosAnualPercentual: cenario.taxaModo === 'manual' ? parsePercent(cenario.taxaManual)! : undefined,
                        percentualCdi: cenario.taxaModo === 'cdi' ? parsePercent(cenario.percentualCdi)! : undefined,
                        tipoAtivo: cenario.tipoAtivo,
                    });
                })
            );

            setResultados(cenarios.map((cenario, i) => ({ cenario, resultado: respostas[i] })));
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Não foi possível calcular a comparação. Tente novamente.');
            setResultados(null);
        } finally {
            setIsLoading(false);
        }
    };

    const chartTickFormatter = (mes: number) =>
        prazoMeses > 24 ? `${Math.round(mes / 12)}a` : `${mes}m`;

    const chartData = resultados?.[0]?.resultado.evolucao.map((_, idx) => {
        const ponto: Record<string, number> = { mes: idx + 1 };
        resultados.forEach((r, i) => {
            ponto[`cenario${i}`] = r.resultado.evolucao[idx].valorAcumulado;
        });
        return ponto;
    });

    const melhorIndice = resultados
        ? resultados.reduce((melhor, atual, i, arr) =>
            atual.resultado.valorFinalLiquido > arr[melhor].resultado.valorFinalLiquido ? i : melhor, 0)
        : -1;

    return (
        <div className="proj-container">
            <form className="proj-form" onSubmit={handleSubmit}>
                <div className="proj-form-row">
                    <div className="proj-form-group">
                        <label htmlFor="cmpAporteInicial">Aporte inicial (R$)</label>
                        <input
                            id="cmpAporteInicial"
                            type="text"
                            inputMode="numeric"
                            placeholder="0,00"
                            value={aporteInicial}
                            onChange={e => setAporteInicial(maskCurrency(e.target.value))}
                            disabled={isLoading}
                        />
                    </div>
                    <div className="proj-form-group">
                        <label htmlFor="cmpAporteMensal">Aporte mensal (R$)</label>
                        <input
                            id="cmpAporteMensal"
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
                    <label htmlFor="cmpPrazo">Prazo (igual para todos os cenários)</label>
                    <div className="proj-prazo-row">
                        <input
                            id="cmpPrazo"
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

                <div className="cmp-cenarios-grid">
                    {cenarios.map((cenario, i) => (
                        <div key={cenario.id} className="cmp-cenario-card" style={{ borderTopColor: CORES_CENARIO[i] }}>
                            <div className="cmp-cenario-header">
                                <span className="cmp-cenario-dot" style={{ background: CORES_CENARIO[i] }} aria-hidden="true" />
                                <span className="cmp-cenario-titulo">Cenário {i + 1}</span>
                                {cenarios.length > MIN_CENARIOS && (
                                    <button
                                        type="button"
                                        className="cmp-cenario-remover"
                                        onClick={() => removerCenario(cenario.id)}
                                        disabled={isLoading}
                                        aria-label={`Remover cenário ${i + 1}`}
                                    >
                                        ✕
                                    </button>
                                )}
                            </div>

                            <label htmlFor={`cmpTipo-${cenario.id}`}>Tipo de ativo</label>
                            <select
                                id={`cmpTipo-${cenario.id}`}
                                value={cenario.tipoAtivo}
                                onChange={e => atualizarCenario(cenario.id, { tipoAtivo: Number(e.target.value) as TipoAtivoCalculadora })}
                                disabled={isLoading}
                            >
                                {GRUPOS_TIPO_ATIVO.map(grupo => (
                                    <optgroup key={grupo} label={GRUPO_TIPO_ATIVO_LABEL[grupo]}>
                                        {tiposAtivoPorGrupo(grupo).map(tipo => (
                                            <option key={tipo} value={tipo}>
                                                {TIPO_ATIVO_CALCULADORA_META[tipo].label}
                                            </option>
                                        ))}
                                    </optgroup>
                                ))}
                            </select>

                            <label>Taxa de juros</label>
                            <div className="proj-toggle-group proj-toggle-group--full">
                                <button
                                    type="button"
                                    className={`proj-toggle-btn${cenario.taxaModo === 'selic' ? ' proj-toggle-btn--active' : ''}`}
                                    onClick={() => atualizarCenario(cenario.id, { taxaModo: 'selic' })}
                                    disabled={isLoading}
                                >
                                    Selic
                                </button>
                                <button
                                    type="button"
                                    className={`proj-toggle-btn${cenario.taxaModo === 'cdi' ? ' proj-toggle-btn--active' : ''}`}
                                    onClick={() => atualizarCenario(cenario.id, { taxaModo: 'cdi' })}
                                    disabled={isLoading}
                                >
                                    % CDI
                                </button>
                                <button
                                    type="button"
                                    className={`proj-toggle-btn${cenario.taxaModo === 'manual' ? ' proj-toggle-btn--active' : ''}`}
                                    onClick={() => atualizarCenario(cenario.id, { taxaModo: 'manual' })}
                                    disabled={isLoading}
                                >
                                    Manual
                                </button>
                            </div>
                            {cenario.taxaModo === 'manual' && (
                                <input
                                    className="proj-taxa-manual-input"
                                    type="text"
                                    inputMode="decimal"
                                    placeholder="Ex: 6,17 (% ao ano)"
                                    value={cenario.taxaManual}
                                    onChange={e => atualizarCenario(cenario.id, { taxaManual: e.target.value })}
                                    disabled={isLoading}
                                />
                            )}
                            {cenario.taxaModo === 'cdi' && (
                                <input
                                    className="proj-taxa-manual-input"
                                    type="text"
                                    inputMode="decimal"
                                    placeholder="Ex: 100 (% do CDI)"
                                    value={cenario.percentualCdi}
                                    onChange={e => atualizarCenario(cenario.id, { percentualCdi: e.target.value })}
                                    disabled={isLoading}
                                />
                            )}
                        </div>
                    ))}

                    {cenarios.length < MAX_CENARIOS && (
                        <button
                            type="button"
                            className="cmp-cenario-adicionar"
                            onClick={adicionarCenario}
                            disabled={isLoading}
                        >
                            + Adicionar cenário
                        </button>
                    )}
                </div>

                {error && <span className="proj-error">{error}</span>}

                <button type="submit" className="proj-btn-submit" disabled={isLoading}>
                    {isLoading ? 'Calculando...' : 'Comparar cenários'}
                </button>
            </form>

            {resultados && (
                <div className="proj-result">
                    <div className="cmp-tabela-wrap">
                        <table className="cmp-tabela">
                            <thead>
                                <tr>
                                    <th>Cenário</th>
                                    {resultados.map((r, i) => (
                                        <th key={r.cenario.id}>
                                            <span className="cmp-th-dot" style={{ background: CORES_CENARIO[i] }} aria-hidden="true" />
                                            {TIPO_ATIVO_CALCULADORA_META[r.cenario.tipoAtivo].label}
                                            {i === melhorIndice && <span className="cmp-badge-melhor">melhor</span>}
                                        </th>
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>Taxa efetiva (a.a.)</td>
                                    {resultados.map(r => (
                                        <td key={r.cenario.id}>{r.resultado.taxaJurosAnualUtilizada.toFixed(2)}%</td>
                                    ))}
                                </tr>
                                <tr>
                                    <td>Total aportado</td>
                                    {resultados.map(r => (
                                        <td key={r.cenario.id}>{formatCurrency(r.resultado.totalAportado)}</td>
                                    ))}
                                </tr>
                                <tr>
                                    <td>Total de tributos</td>
                                    {resultados.map(r => (
                                        <td key={r.cenario.id} className="cmp-td-red">
                                            -{formatCurrency(r.resultado.valorIof + r.resultado.valorComeCotasRetido + r.resultado.valorImpostoRenda)}
                                        </td>
                                    ))}
                                </tr>
                                <tr>
                                    <td>Rentabilidade líquida</td>
                                    {resultados.map(r => (
                                        <td key={r.cenario.id}>
                                            {(((r.resultado.valorFinalLiquido - r.resultado.totalAportado) / r.resultado.totalAportado) * 100).toFixed(2)}%
                                        </td>
                                    ))}
                                </tr>
                                {resultados.some(r => r.resultado.rentabilidadeRealAnualPercentual != null) && (
                                    <tr>
                                        <td>Rentabilidade real (a.a.)</td>
                                        {resultados.map(r => (
                                            <td key={r.cenario.id}>
                                                {r.resultado.rentabilidadeRealAnualPercentual != null
                                                    ? `${r.resultado.rentabilidadeRealAnualPercentual.toFixed(2)}%`
                                                    : '—'}
                                            </td>
                                        ))}
                                    </tr>
                                )}
                                <tr className="cmp-row-highlight">
                                    <td>Valor final líquido</td>
                                    {resultados.map((r, i) => (
                                        <td key={r.cenario.id} className={i === melhorIndice ? 'cmp-td-melhor' : undefined}>
                                            {formatCurrency(r.resultado.valorFinalLiquido)}
                                        </td>
                                    ))}
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <div className="proj-chart">
                        <ResponsiveContainer width="100%" height={320}>
                            <ComposedChart data={chartData} margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
                                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                                <XAxis
                                    dataKey="mes"
                                    tickFormatter={chartTickFormatter}
                                    interval={Math.max(0, Math.ceil((chartData?.length ?? 0) / 10) - 1)}
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
                                    formatter={(value, _name, item) => {
                                        const idx = Number(String(item.dataKey).replace('cenario', ''));
                                        const label = TIPO_ATIVO_CALCULADORA_META[resultados[idx].cenario.tipoAtivo].label;
                                        return [formatCurrency(Number(value)), label];
                                    }}
                                    labelFormatter={(mes) => `Mês ${mes}`}
                                />
                                <Legend
                                    formatter={(_value, entry) => {
                                        const idx = Number(String((entry as { dataKey?: string }).dataKey ?? '').replace('cenario', ''));
                                        return TIPO_ATIVO_CALCULADORA_META[resultados[idx]?.cenario.tipoAtivo]?.label ?? '';
                                    }}
                                />
                                {resultados.map((_r, i) => (
                                    <Line
                                        key={i}
                                        type="monotone"
                                        dataKey={`cenario${i}`}
                                        stroke={CORES_CENARIO[i]}
                                        strokeWidth={2}
                                        dot={false}
                                    />
                                ))}
                            </ComposedChart>
                        </ResponsiveContainer>
                    </div>
                </div>
            )}
        </div>
    );
}
