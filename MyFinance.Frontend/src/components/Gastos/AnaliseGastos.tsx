import { useEffect, useMemo, useRef, useState } from 'react';
import { accountService, analyticsService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import type { ExpenseOverviewResponseDto, ExpenseTimelineResponseDto } from '../../types/ExpenseAnalytics';
import { GastosFiltros } from './GastosFiltros';
import { GastosKpiRow } from './GastosKpiRow';
import { GastosPorCategoria } from './GastosPorCategoria';
import { GastosEvolucao } from './GastosEvolucao';
import { GastosMaioresLancamentos } from './GastosMaioresLancamentos';
import { GastosSkeleton } from './GastosSkeleton';
import { construirRanking, descreverIntervalo, calcularMesParcial, type PeriodoPreset } from './gastosSelectors';
import { Alerta, EstadoVazio } from '../Shared/ui';
import './AnaliseGastos.css';

const PRESET_LABEL: Record<PeriodoPreset, string> = {
    '3m': '3 meses', '6m': '6 meses', '12m': '12 meses', ano: 'este ano', custom: 'personalizado',
};

interface Intervalo {
    startDate: string;
    endDate: string;
    months: number;
}

const isoDate = (d: Date) => d.toISOString().slice(0, 10);

/** Resolve o preset (ou o intervalo personalizado) num range de datas + nº de meses para a timeline. */
function calcularIntervalo(preset: PeriodoPreset, customStart: string, customEnd: string): Intervalo | null {
    const hoje = new Date();
    const fim = new Date(Date.UTC(hoje.getFullYear(), hoje.getMonth(), hoje.getDate()));

    switch (preset) {
        case '3m':
            return { startDate: isoDate(new Date(Date.UTC(fim.getUTCFullYear(), fim.getUTCMonth() - 2, 1))), endDate: isoDate(fim), months: 3 };
        case '6m':
            return { startDate: isoDate(new Date(Date.UTC(fim.getUTCFullYear(), fim.getUTCMonth() - 5, 1))), endDate: isoDate(fim), months: 6 };
        case '12m':
            return { startDate: isoDate(new Date(Date.UTC(fim.getUTCFullYear(), fim.getUTCMonth() - 11, 1))), endDate: isoDate(fim), months: 12 };
        case 'ano':
            return { startDate: isoDate(new Date(Date.UTC(fim.getUTCFullYear(), 0, 1))), endDate: isoDate(fim), months: fim.getUTCMonth() + 1 };
        case 'custom': {
            if (!customStart || !customEnd) return null;
            const inicio = new Date(`${customStart}T00:00:00Z`);
            const termino = new Date(`${customEnd}T00:00:00Z`);
            const months = Math.max(1, Math.min(36,
                (termino.getUTCFullYear() - inicio.getUTCFullYear()) * 12 + (termino.getUTCMonth() - inicio.getUTCMonth()) + 1));
            return { startDate: customStart, endDate: customEnd, months };
        }
    }
}

/** "2026-08" → "agosto". */
function nomeDoMes(label: string): string {
    const [ano, mes] = label.split('-').map(Number);
    const data = new Date(Date.UTC(ano, mes - 1, 1));
    return new Intl.DateTimeFormat('pt-BR', { month: 'long', timeZone: 'UTC' }).format(data);
}

/**
 * Container da aba "Gastos" do dashboard: filtro de período/conta em uma única linha, fixo no
 * topo (regra da skill dataviz — filtros escopam tudo abaixo, nunca por gráfico), seguido de KPIs
 * e três cards de largura total (categoria, evolução com seletor de visão, maiores lançamentos).
 * Único componente da seção que fala com a API.
 */
export function AnaliseGastos() {
    const [preset, setPreset] = useState<PeriodoPreset>('3m');
    const [customStart, setCustomStart] = useState('');
    const [customEnd, setCustomEnd] = useState('');
    const [accounts, setAccounts] = useState<AccountResponseDto[]>([]);
    const [accountId, setAccountId] = useState('');

    const [overview, setOverview] = useState<ExpenseOverviewResponseDto | null>(null);
    const [timeline, setTimeline] = useState<ExpenseTimelineResponseDto | null>(null);
    const [isLoading, setLoading] = useState(true);
    const [isRefetching, setRefetching] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [refetchToken, setRefetchToken] = useState(0);
    const carregouAntes = useRef(false);

    useEffect(() => {
        accountService.getAllAccounts()
            .then(response => setAccounts(response.data))
            .catch(() => { /* seletor de conta é opcional — falha aqui não bloqueia a análise */ });
    }, []);

    const intervalo = useMemo(() => calcularIntervalo(preset, customStart, customEnd), [preset, customStart, customEnd]);

    useEffect(() => {
        if (!intervalo) return;

        let cancelado = false;
        if (carregouAntes.current) setRefetching(true); else setLoading(true);
        setError(null);

        const params = { startDate: intervalo.startDate, endDate: intervalo.endDate, accountId: accountId || undefined };

        Promise.all([
            analyticsService.getExpenseOverview(params),
            analyticsService.getExpenseTimeline({ ...params, months: intervalo.months }),
        ])
            .then(([overviewData, timelineData]) => {
                if (cancelado) return;
                setOverview(overviewData);
                setTimeline(timelineData);
                carregouAntes.current = true;
            })
            .catch(err => {
                if (cancelado) return;
                const axiosError = err as AxiosError<ApiErrorResponse>;
                setError(axiosError.response?.data?.message || 'Não foi possível carregar a análise de gastos. Tente novamente mais tarde.');
                // Mantém overview/timeline anteriores (se houver) de propósito — o
                // card de erro deixa claro que o que está na tela pode estar
                // desatualizado, em vez de trocar tudo por uma aba em branco.
            })
            .finally(() => {
                if (cancelado) return;
                setLoading(false);
                setRefetching(false);
            });

        return () => { cancelado = true; };
    }, [intervalo, accountId, refetchToken]);

    // Ranking único da aba — donut, ranking e evolução por composição/tendência
    // usam exatamente este corte, em vez de cada um calcular o seu.
    const ranking = useMemo(() => (overview ? construirRanking(overview.categories) : null), [overview]);

    const mesParcialLabel = intervalo ? calcularMesParcial(preset, intervalo.endDate) : null;
    const nomeMesParcial = mesParcialLabel ? nomeDoMes(mesParcialLabel) : null;
    const legendaIntervalo = intervalo ? `${descreverIntervalo(intervalo.startDate, intervalo.endDate)} · ${PRESET_LABEL[preset]}` : null;
    const avisoMesParcial = nomeMesParcial ? `${nomeMesParcial} ainda em curso` : null;
    const dadosDesatualizados = !!error && !!overview;

    return (
        <div className="gastos-container">
            <GastosFiltros
                preset={preset}
                onPresetChange={setPreset}
                customStart={customStart}
                customEnd={customEnd}
                onCustomStartChange={setCustomStart}
                onCustomEndChange={setCustomEnd}
                accounts={accounts}
                accountId={accountId}
                onAccountChange={setAccountId}
                legenda={legendaIntervalo}
                avisoMesParcial={avisoMesParcial}
            />

            {error && (
                <Alerta rotuloAcao="Tentar novamente" onAcao={() => setRefetchToken(t => t + 1)}>
                    {error}{dadosDesatualizados && ' Os dados abaixo podem estar desatualizados.'}
                </Alerta>
            )}

            {isLoading ? (
                <GastosSkeleton />
            ) : preset === 'custom' && !intervalo ? (
                <EstadoVazio
                    variante="hero"
                    icone="📅"
                    titulo="Selecione um período"
                    descricao="Escolha a data inicial e a data final para analisar seus gastos."
                />
            ) : overview && timeline && ranking ? (
                <div className={`gastos-content${isRefetching || dadosDesatualizados ? ' gastos-content--loading' : ''}`}>
                    {overview.totalExpenses === 0 && overview.totalIncome === 0 ? (
                        <EstadoVazio
                            variante="hero"
                            icone="💸"
                            titulo="Nenhuma movimentação no período"
                            descricao={<>Não encontramos despesas ou receitas nesse intervalo. Importe seu extrato na página <strong>Início</strong> ou ajuste o período acima.</>}
                        />
                    ) : (
                        <>
                            <GastosKpiRow overview={overview} nomeMesParcial={nomeMesParcial} />
                            <GastosPorCategoria overview={overview} ranking={ranking} />
                            <GastosEvolucao overview={overview} timeline={timeline} ranking={ranking} />
                            <GastosMaioresLancamentos overview={overview} />
                        </>
                    )}
                </div>
            ) : error ? (
                <EstadoVazio variante="hero" icone="⚠️" titulo="Não foi possível carregar a análise" descricao="Tente novamente em instantes." />
            ) : null}
        </div>
    );
}
