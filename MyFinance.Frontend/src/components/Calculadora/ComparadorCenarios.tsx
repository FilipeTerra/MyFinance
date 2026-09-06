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
// .tabela/.tabela-wrap/.tabela-badge etc — não confiar em outro componente
// carregar isto incidentalmente.
import '../Shared/ui/Tabela.css';
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

    /**
     * Fonte única das métricas comparadas — consumida pela tabela (desktop,
     * uma coluna por cenário) e pelos cartões (celular, um cartão por
     * cenário). Sem isto, a tabela virar cartões abaixo de 768px exigiria
     * copiar cada fórmula duas vezes, com risco real de as duas cópias
     * divergirem numa correção futura.
     */
    const linhasComparacao: {
        chave: string;
        rotulo: string;
        valor: (r: ResultadoCenario) => string;
        tom?: 'red';
        destaque?: boolean;
    }[] = resultados
        ? [
            { chave: 'taxa', rotulo: 'Taxa efetiva (a.a.)', valor: r => `${r.resultado.taxaJurosAnualUtilizada.toFixed(2)}%` },
            { chave: 'aportado', rotulo: 'Total aportado', valor: r => formatCurrency(r.resultado.totalAportado) },
            {
                chave: 'tributos',
                rotulo: 'Total de tributos',
                valor: r => `-${formatCurrency(r.resultado.valorIof + r.resultado.valorComeCotasRetido + r.resultado.valorImpostoRenda)}`,
                tom: 'red',
            },
            {
                chave: 'rentLiquida',
                rotulo: 'Rentabilidade líquida',
                valor: r => `${(((r.resultado.valorFinalLiquido - r.resultado.totalAportado) / r.resultado.totalAportado) * 100).toFixed(2)}%`,
            },
            ...(resultados.some(r => r.resultado.rentabilidadeRealAnualPercentual != null)
                ? [{
                    chave: 'rentReal',
                    rotulo: 'Rentabilidade real (a.a.)',
                    valor: (r: ResultadoCenario) => r.resultado.rentabilidadeRealAnualPercentual != null
                        ? `${r.resultado.rentabilidadeRealAnualPercentual.toFixed(2)}%`
                        : '—',
                }]
                : []),
            { chave: 'valorFinal', rotulo: 'Valor final líquido', valor: r => formatCurrency(r.resultado.valorFinalLiquido), destaque: true },
        ]
        : [];

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
                    {/* Desktop: tabela com uma coluna por cenário — comparar lado a lado é o
                        ponto desta tela, e cabe bem em telas largas. */}
                    <div className="tabela-wrap">
                        <table className="tabela tabela--numerica">
                            <thead>
                                <tr>
                                    <th>Cenário</th>
                                    {resultados.map((r, i) => (
                                        <th key={r.cenario.id}>
                                            <span className="tabela-th-dot" style={{ background: CORES_CENARIO[i] }} aria-hidden="true" />
                                            {TIPO_ATIVO_CALCULADORA_META[r.cenario.tipoAtivo].label}
                                            {i === melhorIndice && <span className="tabela-badge">melhor</span>}
                                        </th>
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                {linhasComparacao.map(linha => (
                                    <tr key={linha.chave} className={linha.destaque ? 'cmp-row-highlight' : undefined}>
                                        <td>{linha.rotulo}</td>
                                        {resultados.map((r, i) => (
                                            <td
                                                key={r.cenario.id}
                                                className={
                                                    linha.destaque && i === melhorIndice
                                                        ? 'cmp-td-melhor'
                                                        : linha.tom === 'red' ? 'destaque-negativo' : undefined
                                                }
                                            >
                                                {linha.valor(r)}
                                            </td>
                                        ))}
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    {/* Celular: um cartão por cenário. A tabela acima é transposta —
                        coluna vira cartão, linha vira rótulo dentro dele — não dá para
                        chegar aqui só com CSS a partir do <table>; os cartões usam a
                        mesma `linhasComparacao` para não duplicar nenhuma fórmula. */}
                    <ul className="cmp-cartoes">
                        {resultados.map((r, i) => (
                            <li
                                key={r.cenario.id}
                                className={`cmp-cartao${i === melhorIndice ? ' cmp-cartao--melhor' : ''}`}
                            >
                                <div className="cmp-cartao-cabecalho">
                                    <span className="tabela-th-dot" style={{ background: CORES_CENARIO[i] }} aria-hidden="true" />
                                    <span className="cmp-cartao-titulo">{TIPO_ATIVO_CALCULADORA_META[r.cenario.tipoAtivo].label}</span>
                                    {i === melhorIndice && <span className="tabela-badge">melhor</span>}
                                </div>
                                <dl className="cmp-cartao-lista">
                                    {linhasComparacao.map(linha => (
                                        <div
                                            key={linha.chave}
                                            className={`cmp-cartao-linha${linha.destaque ? ' cmp-cartao-linha--destaque' : ''}`}
                                        >
                                            <dt>{linha.rotulo}</dt>
                                            <dd className={
                                                linha.destaque && i === melhorIndice
                                                    ? 'cmp-td-melhor'
                                                    : linha.tom === 'red' ? 'destaque-negativo' : undefined
                                            }>
                                                {linha.valor(r)}
                                            </dd>
                                        </div>
                                    ))}
                                </dl>
                            </li>
                        ))}
                    </ul>

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
