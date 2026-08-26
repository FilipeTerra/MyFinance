import { useEffect, useMemo, useState } from 'react';
import { Header } from '../components/Layout/Header';
import { FinancialGoalCard } from '../components/FinancialGoals/FinancialGoalCard';
import { InvestimentoCard } from '../components/Investimentos/InvestimentoCard';
import { InvestimentoModal } from '../components/Investimentos/InvestimentoModal';
import { CalculadoraProjecao } from '../components/Calculadora/CalculadoraProjecao';
import { InsightCard } from '../components/Shared/InsightCard';
import { financialGoalService, investimentoService, aiService } from '../services/Api';
import type { FinancialGoalResponseDto } from '../types/FinancialGoalResponseDto';
import type { InvestimentoResponseDto } from '../types/InvestimentoResponseDto';
import type { ProactiveInsightResponseDto } from '../types/AiIntegration';
import './DashboardPage.css';

type DashboardTab = 'metas' | 'investimentos' | 'calculadora';

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

function computeGoalStats(goals: FinancialGoalResponseDto[]) {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return {
        total:       goals.length,
        completed:   goals.filter(g => g.isCompleted).length,
        inProgress:  goals.filter(g => !g.isCompleted && new Date(g.deadline) >= today).length,
        overdue:     goals.filter(g => !g.isCompleted && new Date(g.deadline) < today).length,
        totalSaved:  goals.reduce((s, g) => s + g.currentAmount, 0),
        totalTarget: goals.reduce((s, g) => s + g.targetAmount, 0),
    };
}

function computeInvestStats(items: InvestimentoResponseDto[]) {
    const totalInvestido = items.reduce((s, i) => s + i.totalAportado, 0);
    const totalAtual     = items.reduce((s, i) => s + i.valorAtual, 0);
    const resultado      = totalAtual - totalInvestido;
    return {
        total:         items.length,
        totalInvestido,
        totalAtual,
        resultado,
        rentabilidade: totalInvestido === 0 ? 0 : (resultado / totalInvestido) * 100,
    };
}

export function DashboardPage() {
    const [activeTab, setActiveTab] = useState<DashboardTab>('metas');

    const [goals, setGoals]                 = useState<FinancialGoalResponseDto[]>([]);
    const [investimentos, setInvestimentos] = useState<InvestimentoResponseDto[]>([]);
    const [isLoading, setLoading]           = useState(true);
    const [error, setError]                 = useState<string | null>(null);
    const [isCreateOpen, setCreateOpen]     = useState(false);
    const [insight, setInsight]             = useState<ProactiveInsightResponseDto | null>(null);

    const fetchGoals = async () => {
        const data = await financialGoalService.getAll();
        setGoals(data);
    };

    const fetchInvestimentos = async () => {
        const data = await investimentoService.getAll();
        setInvestimentos(data);
    };

    const fetchAll = async () => {
        setLoading(true);
        setError(null);
        try {
            await Promise.all([fetchGoals(), fetchInvestimentos()]);
        } catch {
            setError('Não foi possível carregar seus dados. Tente novamente mais tarde.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        void fetchAll();
    }, []);

    // Análise proativa do Agente IA: carrega em silêncio, sem bloquear o
    // dashboard e sem exibir erro ao usuário caso falhe (non-blocking).
    useEffect(() => {
        aiService.getEmergencyReserveInsight()
            .then(result => {
                if (result.success && result.showCard && result.information) {
                    setInsight(result);
                }
            })
            .catch(err => {
                console.warn('[AI] Falha ao buscar insight proativo (non-blocking):', err);
            });
    }, []);

    const handleContributionSuccess = async () => {
        alert('Aporte realizado com sucesso! A barra de progresso foi atualizada.');
        await fetchGoals();
    };

    const goalStats   = useMemo(() => computeGoalStats(goals), [goals]);
    const investStats = useMemo(() => computeInvestStats(investimentos), [investimentos]);
    const resultTrend = investStats.resultado > 0 ? 'green' : investStats.resultado < 0 ? 'red' : 'flat';

    const subtitle = activeTab === 'metas'
        ? 'Acompanhe a evolução dos seus objetivos financeiros'
        : activeTab === 'investimentos'
        ? 'Gerencie sua carteira e acompanhe a rentabilidade dos seus ativos'
        : 'Simule quanto você pode acumular investindo a longo prazo';

    return (
        <div className="dashboard-container">
            <Header />

            <main className="dashboard-content">
                <div className="dashboard-page-header">
                    <div>
                        <h2 className="dashboard-title">Meu Painel Financeiro</h2>
                        <p className="dashboard-subtitle">{subtitle}</p>
                    </div>
                    {activeTab === 'investimentos' && (
                        <button className="dashboard-primary-btn" onClick={() => setCreateOpen(true)}>
                            <span aria-hidden="true">＋</span> Novo investimento
                        </button>
                    )}
                </div>

                {/* ── Segmented tabs ─────────────────────────────────────────── */}
                <div className="dashboard-tabs" role="tablist" aria-label="Seções do painel">
                    <button
                        role="tab"
                        aria-selected={activeTab === 'metas'}
                        className={`dashboard-tab${activeTab === 'metas' ? ' dashboard-tab--active' : ''}`}
                        onClick={() => setActiveTab('metas')}
                    >
                        <span className="dashboard-tab-icon" aria-hidden="true">🎯</span>
                        Metas
                        {goals.length > 0 && <span className="dashboard-tab-count">{goals.length}</span>}
                    </button>
                    <button
                        role="tab"
                        aria-selected={activeTab === 'investimentos'}
                        className={`dashboard-tab${activeTab === 'investimentos' ? ' dashboard-tab--active' : ''}`}
                        onClick={() => setActiveTab('investimentos')}
                    >
                        <span className="dashboard-tab-icon" aria-hidden="true">📈</span>
                        Investimentos
                        {investimentos.length > 0 && <span className="dashboard-tab-count">{investimentos.length}</span>}
                    </button>
                    <button
                        role="tab"
                        aria-selected={activeTab === 'calculadora'}
                        className={`dashboard-tab${activeTab === 'calculadora' ? ' dashboard-tab--active' : ''}`}
                        onClick={() => setActiveTab('calculadora')}
                    >
                        <span className="dashboard-tab-icon" aria-hidden="true">🧮</span>
                        Calculadora
                    </button>
                </div>

                {insight && insight.cardType && insight.curiosity && insight.information && insight.suggestion && (
                    <InsightCard
                        cardType={insight.cardType}
                        curiosity={insight.curiosity}
                        information={insight.information}
                        suggestion={insight.suggestion}
                        idealAmount={insight.idealAmount}
                        percentAchieved={insight.percentAchieved}
                        onDismiss={() => setInsight(null)}
                        onGoalCreated={() => {
                            setActiveTab('metas');
                            void fetchGoals();
                        }}
                    />
                )}

                {error && <div className="dashboard-error">{error}</div>}

                {/* ═══ Metas ═════════════════════════════════════════════════ */}
                {activeTab === 'metas' && (
                    <>
                        {!isLoading && goals.length > 0 && (
                            <div className="dashboard-summary">
                                <div className="summary-stat">
                                    <span className="summary-stat-value">{goalStats.total}</span>
                                    <span className="summary-stat-label">Total de metas</span>
                                </div>
                                <div className="summary-divider" />
                                <div className="summary-stat">
                                    <span className="summary-stat-value summary-stat-value--green">{goalStats.completed}</span>
                                    <span className="summary-stat-label">Concluídas</span>
                                </div>
                                <div className="summary-divider" />
                                <div className="summary-stat">
                                    <span className="summary-stat-value summary-stat-value--blue">{goalStats.inProgress}</span>
                                    <span className="summary-stat-label">Em andamento</span>
                                </div>
                                {goalStats.overdue > 0 && (
                                    <>
                                        <div className="summary-divider" />
                                        <div className="summary-stat">
                                            <span className="summary-stat-value summary-stat-value--amber">{goalStats.overdue}</span>
                                            <span className="summary-stat-label">Atrasadas</span>
                                        </div>
                                    </>
                                )}
                                <div className="summary-divider summary-divider--grow" />
                                <div className="summary-stat summary-stat--right">
                                    <span className="summary-stat-value">{formatCurrency(goalStats.totalSaved)}</span>
                                    <span className="summary-stat-label">de {formatCurrency(goalStats.totalTarget)} acumulados</span>
                                </div>
                            </div>
                        )}

                        {isLoading ? (
                            <div className="goals-skeleton-grid">
                                {[1, 2, 3].map(i => <div key={i} className="goal-skeleton" />)}
                            </div>
                        ) : goals.length === 0 ? (
                            <div className="dashboard-empty">
                                <div className="dashboard-empty-icon" aria-hidden="true">◎</div>
                                <h3 className="dashboard-empty-title">Nenhuma meta cadastrada</h3>
                                <p className="dashboard-empty-desc">
                                    Converse com o <strong>Assistente IA</strong> e peça para criar
                                    sua primeira meta financeira. Ele vai te ajudar a definir valores
                                    e prazos realistas.
                                </p>
                            </div>
                        ) : (
                            <div className="goals-grid">
                                {goals.map(goal => (
                                    <FinancialGoalCard
                                        key={goal.id}
                                        goal={goal}
                                        onContributionSuccess={handleContributionSuccess}
                                        onDeleteSuccess={fetchGoals}
                                    />
                                ))}
                            </div>
                        )}
                    </>
                )}

                {/* ═══ Investimentos ═════════════════════════════════════════ */}
                {activeTab === 'investimentos' && (
                    <>
                        {!isLoading && investimentos.length > 0 && (
                            <div className="dashboard-summary">
                                <div className="summary-stat">
                                    <span className="summary-stat-value">{investStats.total}</span>
                                    <span className="summary-stat-label">Ativos</span>
                                </div>
                                <div className="summary-divider" />
                                <div className="summary-stat">
                                    <span className="summary-stat-value">{formatCurrency(investStats.totalInvestido)}</span>
                                    <span className="summary-stat-label">Total aportado</span>
                                </div>
                                <div className="summary-divider" />
                                <div className="summary-stat">
                                    <span className="summary-stat-value">{formatCurrency(investStats.totalAtual)}</span>
                                    <span className="summary-stat-label">Valor atual</span>
                                </div>
                                <div className="summary-divider summary-divider--grow" />
                                <div className="summary-stat summary-stat--right">
                                    <span className={`summary-stat-value summary-stat-value--${resultTrend === 'green' ? 'green' : resultTrend === 'red' ? 'red' : 'flat'}`}>
                                        {investStats.resultado >= 0 ? '+' : ''}{formatCurrency(investStats.resultado)}
                                    </span>
                                    <span className="summary-stat-label">
                                        Resultado ({investStats.rentabilidade >= 0 ? '+' : ''}{investStats.rentabilidade.toFixed(2)}%)
                                    </span>
                                </div>
                            </div>
                        )}

                        {isLoading ? (
                            <div className="goals-skeleton-grid">
                                {[1, 2, 3].map(i => <div key={i} className="goal-skeleton" />)}
                            </div>
                        ) : investimentos.length === 0 ? (
                            <div className="dashboard-empty">
                                <div className="dashboard-empty-icon" aria-hidden="true">📈</div>
                                <h3 className="dashboard-empty-title">Nenhum investimento cadastrado</h3>
                                <p className="dashboard-empty-desc">
                                    Comece a montar sua carteira. Clique em <strong>Novo investimento</strong> para
                                    registrar sua primeira aplicação e acompanhar a rentabilidade ao longo do tempo.
                                </p>
                                <button
                                    className="dashboard-primary-btn dashboard-empty-btn"
                                    onClick={() => setCreateOpen(true)}
                                >
                                    <span aria-hidden="true">＋</span> Novo investimento
                                </button>
                            </div>
                        ) : (
                            <div className="goals-grid">
                                {investimentos.map(inv => (
                                    <InvestimentoCard
                                        key={inv.id}
                                        investimento={inv}
                                        onUpdateSuccess={fetchInvestimentos}
                                        onDeleteSuccess={fetchInvestimentos}
                                    />
                                ))}
                            </div>
                        )}
                    </>
                )}

                {/* ═══ Calculadora ═══════════════════════════════════════════ */}
                {activeTab === 'calculadora' && <CalculadoraProjecao />}
            </main>

            {isCreateOpen && (
                <InvestimentoModal
                    mode="create"
                    onClose={() => setCreateOpen(false)}
                    onSuccess={() => {
                        setCreateOpen(false);
                        void fetchInvestimentos();
                    }}
                />
            )}
        </div>
    );
}
