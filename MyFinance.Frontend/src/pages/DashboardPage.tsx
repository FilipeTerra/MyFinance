import { useEffect, useMemo, useState } from 'react';
import { Header } from '../components/Layout/Header';
import { FinancialGoalCard } from '../components/FinancialGoals/FinancialGoalCard';
import { financialGoalService } from '../services/Api';
import type { FinancialGoalResponseDto } from '../types/FinancialGoalResponseDto';
import './DashboardPage.css';

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

function computeStats(goals: FinancialGoalResponseDto[]) {
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

export function DashboardPage() {
    const [goals, setGoals]       = useState<FinancialGoalResponseDto[]>([]);
    const [isLoading, setLoading] = useState(true);
    const [error, setError]       = useState<string | null>(null);

    const fetchGoals = async () => {
        setLoading(true);
        try {
            const data = await financialGoalService.getAll();
            setGoals(data);
        } catch {
            setError('Não foi possível carregar as metas. Tente novamente mais tarde.');
        } finally {
            setLoading(false);
        }
    };

    const handleContributionSuccess = async () => {
        alert('Aporte realizado com sucesso! A barra de progresso foi atualizada.');
        await fetchGoals();
    };

    const handleGoalDeleted = async () => {
        await fetchGoals();
    };

    useEffect(() => {
        void fetchGoals();
    }, []);

    const stats = useMemo(() => computeStats(goals), [goals]);

    return (
        <div className="dashboard-container">
            <Header />

            <main className="dashboard-content">
                <div className="dashboard-page-header">
                    <div>
                        <h2 className="dashboard-title">Minhas Metas</h2>
                        <p className="dashboard-subtitle">Acompanhe a evolução dos seus objetivos financeiros</p>
                    </div>
                </div>

                {error && <div className="dashboard-error">{error}</div>}

                {!isLoading && goals.length > 0 && (
                    <div className="dashboard-summary">
                        <div className="summary-stat">
                            <span className="summary-stat-value">{stats.total}</span>
                            <span className="summary-stat-label">Total de metas</span>
                        </div>
                        <div className="summary-divider" />
                        <div className="summary-stat">
                            <span className="summary-stat-value summary-stat-value--green">{stats.completed}</span>
                            <span className="summary-stat-label">Concluídas</span>
                        </div>
                        <div className="summary-divider" />
                        <div className="summary-stat">
                            <span className="summary-stat-value summary-stat-value--blue">{stats.inProgress}</span>
                            <span className="summary-stat-label">Em andamento</span>
                        </div>
                        {stats.overdue > 0 && (
                            <>
                                <div className="summary-divider" />
                                <div className="summary-stat">
                                    <span className="summary-stat-value summary-stat-value--amber">{stats.overdue}</span>
                                    <span className="summary-stat-label">Atrasadas</span>
                                </div>
                            </>
                        )}
                        <div className="summary-divider summary-divider--grow" />
                        <div className="summary-stat summary-stat--right">
                            <span className="summary-stat-value">{formatCurrency(stats.totalSaved)}</span>
                            <span className="summary-stat-label">de {formatCurrency(stats.totalTarget)} acumulados</span>
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
                                onDeleteSuccess={handleGoalDeleted}
                            />
                        ))}
                    </div>
                )}
            </main>
        </div>
    );
}
