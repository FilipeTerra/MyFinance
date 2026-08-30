import { useEffect, useMemo, useRef, useState } from 'react';
import { accountService, analyticsService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import type { ExpenseOverviewResponseDto, ExpenseTimelineResponseDto } from '../../types/ExpenseAnalytics';
import { GastosKpiRow } from './GastosKpiRow';
import { GastosPorCategoria } from './GastosPorCategoria';
import { GastosEvolucaoTemporal } from './GastosEvolucaoTemporal';
import { GastosComparacaoPeriodos } from './GastosComparacaoPeriodos';
import { GastosFluxoMensal } from './GastosFluxoMensal';
import { construirMapaCoresCategorias } from './gastosUtils';
import './AnaliseGastos.css';

type PeriodoPreset = '3m' | '6m' | '12m' | 'ano' | 'custom';

const PRESETS: { value: PeriodoPreset; label: string }[] = [
    { value: '3m', label: '3 meses' },
    { value: '6m', label: '6 meses' },
    { value: '12m', label: '12 meses' },
    { value: 'ano', label: 'Este ano' },
    { value: 'custom', label: 'Personalizado' },
];

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

/**
 * Container da aba "Gastos" do dashboard: filtro de período/conta em uma única linha (regra da
 * skill dataviz — filtros escopam tudo abaixo, nunca por gráfico) e composição dos quatro blocos
 * de análise. Único componente da seção que fala com a API.
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
            })
            .finally(() => {
                if (cancelado) return;
                setLoading(false);
                setRefetching(false);
            });

        return () => { cancelado = true; };
    }, [intervalo, accountId]);

    // Mesma cor para a mesma categoria em todos os gráficos da seção — ranqueada pelo período atual.
    const coresCategorias = useMemo(
        () => (overview ? construirMapaCoresCategorias(overview.categories) : new Map<string, string>()),
        [overview],
    );

    return (
        <div className="gastos-container">
            <div className="gastos-filters">
                <div className="gastos-toggle-group" role="radiogroup" aria-label="Período">
                    {PRESETS.map(p => (
                        <button
                            key={p.value}
                            type="button"
                            role="radio"
                            aria-checked={preset === p.value}
                            className={`gastos-toggle-btn${preset === p.value ? ' gastos-toggle-btn--active' : ''}`}
                            onClick={() => setPreset(p.value)}
                        >
                            {p.label}
                        </button>
                    ))}
                </div>

                {preset === 'custom' && (
                    <div className="gastos-custom-range">
                        <input
                            type="date"
                            value={customStart}
                            max={customEnd || undefined}
                            onChange={e => setCustomStart(e.target.value)}
                            aria-label="Data inicial"
                        />
                        <span className="gastos-custom-range-sep">→</span>
                        <input
                            type="date"
                            value={customEnd}
                            min={customStart || undefined}
                            onChange={e => setCustomEnd(e.target.value)}
                            aria-label="Data final"
                        />
                    </div>
                )}

                {accounts.length > 0 && (
                    <select
                        className="gastos-account-select"
                        value={accountId}
                        onChange={e => setAccountId(e.target.value)}
                        aria-label="Conta"
                    >
                        <option value="">Todas as contas</option>
                        {accounts.map(a => (
                            <option key={a.id} value={a.id}>{a.name}</option>
                        ))}
                    </select>
                )}
            </div>

            {error && <div className="dashboard-error">{error}</div>}

            {isLoading ? (
                <div className="goals-skeleton-grid">
                    {[1, 2, 3, 4].map(i => <div key={i} className="goal-skeleton" />)}
                </div>
            ) : preset === 'custom' && !intervalo ? (
                <div className="dashboard-empty">
                    <div className="dashboard-empty-icon" aria-hidden="true">📅</div>
                    <h3 className="dashboard-empty-title">Selecione um período</h3>
                    <p className="dashboard-empty-desc">Escolha a data inicial e a data final para analisar seus gastos.</p>
                </div>
            ) : overview && timeline ? (
                <div className={`gastos-content${isRefetching ? ' gastos-content--loading' : ''}`}>
                    {overview.totalExpenses === 0 && overview.totalIncome === 0 ? (
                        <div className="dashboard-empty">
                            <div className="dashboard-empty-icon" aria-hidden="true">💸</div>
                            <h3 className="dashboard-empty-title">Nenhuma movimentação no período</h3>
                            <p className="dashboard-empty-desc">
                                Não encontramos despesas ou receitas nesse intervalo. Importe seu extrato na
                                página <strong>Início</strong> ou ajuste o período acima.
                            </p>
                        </div>
                    ) : (
                        <>
                            <GastosKpiRow overview={overview} />

                            <div className="gastos-grid">
                                <GastosPorCategoria overview={overview} coresCategorias={coresCategorias} />
                                <GastosFluxoMensal timeline={timeline} overview={overview} />
                            </div>

                            <GastosEvolucaoTemporal timeline={timeline} coresCategorias={coresCategorias} />

                            <GastosComparacaoPeriodos overview={overview} />
                        </>
                    )}
                </div>
            ) : null}
        </div>
    );
}
