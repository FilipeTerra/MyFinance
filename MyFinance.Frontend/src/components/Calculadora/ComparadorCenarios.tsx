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
import { TIPO_ATIVO_CALCULADORA_META } from './tipoAtivoCalculadoraMeta';
import { formatCurrency, parseCurrency } from './calculadoraUtils';
import { prazoParaMeses, validarPrazo, validarTaxaRendimento, parametrosTaxa } from './calculadoraValidacao';
import type { BaseAportePrazo, TaxaRendimentoValue } from './calculadoraTypes';
import { CampoMoeda } from './campos/CampoMoeda';
import { CampoPrazo } from './campos/CampoPrazo';
import { CampoTaxaRendimento } from './campos/CampoTaxaRendimento';
import { CampoTipoAtivo } from './campos/CampoTipoAtivo';
import { FormFooterCalculadora } from './campos/FormFooterCalculadora';
import { ResultadoSecao } from './campos/ResultadoSecao';
import { useResultadoFoco } from '../../hooks/useResultadoFoco';
import { useErrosFormulario } from '../../hooks/useErrosFormulario';
import { CORES_CATEGORIA, yAxisProps, formatCurrencyCompacta } from '../Shared/charts/chartTheme';
import { useIsMobile } from '../../hooks/useIsMobile';
import './ComparadorCenarios.css';

/** Paleta categórica validada (ordem fixa — nunca reatribuída por rank), a mesma usada em Gastos. */
const CORES_CENARIO = CORES_CATEGORIA;

const MIN_CENARIOS = 2;
const MAX_CENARIOS = 4;

interface CenarioConfig {
    id: string;
    tipoAtivo: TipoAtivoCalculadora;
    taxa: TaxaRendimentoValue;
}

let proximoId = 0;
/** Novo cenário nasce em "selic" — sempre válido sem exigir nenhum campo extra (o modo "manual" já causou submits fadados a falhar por vir com o campo de taxa vazio). */
const novoCenario = (tipoAtivo: TipoAtivoCalculadora): CenarioConfig => ({
    id: `cenario-${proximoId++}`,
    tipoAtivo,
    taxa: { modo: 'selic', taxaManual: '', percentualCdi: '100' },
});

const cenariosIniciais = (): CenarioConfig[] => [
    novoCenario(TipoAtivoCalculadora.TesouroSelic),
    novoCenario(TipoAtivoCalculadora.Cdb),
];

interface ResultadoCenario {
    cenario: CenarioConfig;
    resultado: ProjecaoInvestimentoResponseDto;
}

interface ComparadorCenariosProps {
    base: BaseAportePrazo;
    onBaseChange: (base: BaseAportePrazo) => void;
}

/** Compara até 4 cenários de investimento com o mesmo aporte/prazo — reaproveita aporteInicial/aporteMensal/prazo do "Cenário único" via props, para o usuário não redigitar os mesmos três campos. */
export function ComparadorCenarios({ base, onBaseChange }: ComparadorCenariosProps) {
    const ehMobile = useIsMobile();
    const [cenarios, setCenarios] = useState<CenarioConfig[]>(cenariosIniciais);

    const [isLoading, setIsLoading] = useState(false);
    const [resultados, setResultados] = useState<ResultadoCenario[] | null>(null);
    const { erroGeral, limpar, limparTudo, setErroGeral, definirEFocar } = useErrosFormulario<'prazo'>();
    const resultadoRef = useResultadoFoco(resultados);

    const prazoMeses = prazoParaMeses(base.prazo);

    const atualizarCenario = (id: string, patch: Partial<CenarioConfig>) =>
        setCenarios(prev => prev.map(c => (c.id === id ? { ...c, ...patch } : c)));

    const adicionarCenario = () => {
        if (cenarios.length >= MAX_CENARIOS) return;
        setCenarios(prev => [...prev, novoCenario(TipoAtivoCalculadora.Lci)]);
    };

    const removerCenario = (id: string) => {
        if (cenarios.length <= MIN_CENARIOS) return;
        setCenarios(prev => prev.filter(c => c.id !== id));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        limparTudo();

        const erroPrazo = validarPrazo(base.prazo);
        if (erroPrazo) {
            definirEFocar({ prazo: erroPrazo }, { prazo: 'cmpPrazo' });
            return;
        }

        for (const cenario of cenarios) {
            const erro = validarTaxaRendimento(cenario.taxa);
            if (erro) {
                setErroGeral(`Cenário "${TIPO_ATIVO_CALCULADORA_META[cenario.tipoAtivo].label}": ${erro.charAt(0).toLowerCase()}${erro.slice(1)}`);
                return;
            }
        }

        const aporteInicialNumero = parseCurrency(base.aporteInicial);
        const aporteMensalNumero = parseCurrency(base.aporteMensal);

        setIsLoading(true);
        try {
            const respostas = await Promise.all(
                cenarios.map(cenario => projecaoInvestimentoService.calcular({
                    aporteInicial: aporteInicialNumero,
                    aporteMensal: aporteMensalNumero,
                    prazoMeses,
                    ...parametrosTaxa(cenario.taxa),
                    tipoAtivo: cenario.tipoAtivo,
                }))
            );

            setResultados(cenarios.map((cenario, i) => ({ cenario, resultado: respostas[i] })));
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setErroGeral(axiosError.response?.data?.message || 'Não foi possível calcular a comparação. Tente novamente.');
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
                    <CampoMoeda id="cmpAporteInicial" label="Aporte inicial (R$)" value={base.aporteInicial}
                        onChange={v => onBaseChange({ ...base, aporteInicial: v })} disabled={isLoading} />
                    <CampoMoeda id="cmpAporteMensal" label="Aporte mensal (R$)" value={base.aporteMensal}
                        onChange={v => onBaseChange({ ...base, aporteMensal: v })} disabled={isLoading} />
                </div>

                <CampoPrazo
                    id="cmpPrazo"
                    label="Prazo (igual para todos os cenários)"
                    value={base.prazo}
                    onChange={prazo => { onBaseChange({ ...base, prazo }); limpar('prazo'); }}
                    disabled={isLoading}
                />

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

                            <CampoTipoAtivo
                                id={`cmpTipo-${cenario.id}`}
                                value={cenario.tipoAtivo}
                                onChange={tipoAtivo => atualizarCenario(cenario.id, { tipoAtivo })}
                                mostrarHint={false}
                                disabled={isLoading}
                            />
                            <CampoTaxaRendimento
                                idPrefix={`cmp-${cenario.id}`}
                                value={cenario.taxa}
                                onChange={taxa => atualizarCenario(cenario.id, { taxa })}
                                compacto
                                mostrarHints={false}
                                disabled={isLoading}
                            />
                        </div>
                    ))}

                    {cenarios.length < MAX_CENARIOS && (
                        <button type="button" className="cmp-cenario-adicionar" onClick={adicionarCenario} disabled={isLoading}>
                            + Adicionar cenário
                        </button>
                    )}
                </div>

                <FormFooterCalculadora erro={erroGeral} isLoading={isLoading} rotulo="Comparar cenários" />
            </form>

            {resultados && (
                <ResultadoSecao resultadoRef={resultadoRef}>
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
                        <ResponsiveContainer width="100%" height={ehMobile ? 260 : 320}>
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
                                    tickFormatter={(v: number) => ehMobile ? formatCurrencyCompacta(v) : formatCurrency(v)}
                                    {...yAxisProps(ehMobile)}
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
                </ResultadoSecao>
            )}
        </div>
    );
}
