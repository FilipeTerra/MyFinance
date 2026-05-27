import { useState } from 'react';
import type { FinancialGoalResponseDto } from '../../types/FinancialGoalResponseDto';
import { ContributeToGoalModal } from './ContributeToGoalModal';
import './FinancialGoalCard.css';

interface FinancialGoalCardProps {
    goal: FinancialGoalResponseDto;
    onContributionSuccess: () => void;
}

type GoalStatus = 'completed' | 'almost' | 'in_progress' | 'overdue';

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

const formatDate = (isoString: string) =>
    new Intl.DateTimeFormat('pt-BR').format(new Date(isoString));

function getDaysLeft(deadline: string): number {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const end = new Date(deadline);
    end.setHours(0, 0, 0, 0);
    return Math.round((end.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
}

function getStatus(goal: FinancialGoalResponseDto, progress: number, daysLeft: number): GoalStatus {
    if (goal.isCompleted) return 'completed';
    if (daysLeft < 0) return 'overdue';
    if (progress >= 80) return 'almost';
    return 'in_progress';
}

const STATUS_META: Record<GoalStatus, { label: string; dot: string }> = {
    completed:   { label: 'Concluída',       dot: '✓' },
    almost:      { label: 'Quase lá',        dot: '◎' },
    in_progress: { label: 'Em andamento',    dot: '›' },
    overdue:     { label: 'Prazo encerrado', dot: '!' },
};

function buildInsight(goal: FinancialGoalResponseDto, status: GoalStatus, daysLeft: number): string {
    if (status === 'completed') return 'Meta alcançada com sucesso';
    if (status === 'overdue')   return `Prazo encerrado há ${Math.abs(daysLeft)} dia${Math.abs(daysLeft) !== 1 ? 's' : ''}`;

    const remaining = formatCurrency(goal.targetAmount - goal.currentAmount);
    const timeText =
        daysLeft === 0 ? 'hoje' :
        daysLeft === 1 ? 'amanhã' :
        daysLeft <= 30 ? `${daysLeft} dias` :
        `${Math.round(daysLeft / 30)} ${Math.round(daysLeft / 30) === 1 ? 'mês' : 'meses'}`;

    return `Faltam ${remaining} · Prazo em ${timeText}`;
}

export function FinancialGoalCard({ goal, onContributionSuccess }: FinancialGoalCardProps) {
    const [isContributeModalOpen, setIsContributeModalOpen] = useState(false);

    const progress  = Math.min((goal.currentAmount / goal.targetAmount) * 100, 100);
    const daysLeft  = getDaysLeft(goal.deadline);
    const status    = getStatus(goal, progress, daysLeft);
    const meta      = STATUS_META[status];
    const insight   = buildInsight(goal, status, daysLeft);

    return (
        <div className={`goal-card goal-card--${status}`}>
            <div className="goal-card-header">
                <h3 className="goal-card-name">{goal.name}</h3>
                <span className={`goal-status-badge goal-status-badge--${status}`}>
                    <span className="goal-status-dot" aria-hidden="true">{meta.dot}</span>
                    {meta.label}
                </span>
            </div>

            <div className="goal-progress-section">
                <div className="goal-progress-labels">
                    <span className="goal-pct-value">{progress.toFixed(0)}%</span>
                    <span className="goal-pct-sub">concluído</span>
                </div>
                <div
                    className="goal-progress-track"
                    role="progressbar"
                    aria-valuenow={Math.round(progress)}
                    aria-valuemin={0}
                    aria-valuemax={100}
                    aria-label={`${goal.name}: ${progress.toFixed(0)}% concluído`}
                >
                    <div className="goal-progress-fill" style={{ width: `${progress}%` }} />
                </div>
            </div>

            <div className="goal-stats-row">
                <div className="goal-stat">
                    <span className="goal-stat-label">Atual</span>
                    <span className="goal-stat-value">{formatCurrency(goal.currentAmount)}</span>
                </div>
                <div className="goal-stat-divider" aria-hidden="true" />
                <div className="goal-stat goal-stat--align-end">
                    <span className="goal-stat-label">Meta</span>
                    <span className="goal-stat-value goal-stat-value--muted">{formatCurrency(goal.targetAmount)}</span>
                </div>
            </div>

            <div className="goal-insight">
                <span className="goal-insight-pip" aria-hidden="true" />
                <span className="goal-insight-text">{insight}</span>
            </div>

            <div className="goal-footer">
                <span className="goal-footer-label">Prazo</span>
                <span className="goal-footer-value">{formatDate(goal.deadline)}</span>
            </div>

            <button
                className="goal-contribute-btn"
                onClick={() => setIsContributeModalOpen(true)}
                disabled={goal.isCompleted}
                title={goal.isCompleted ? 'Meta já concluída' : 'Realizar aporte nesta meta'}
            >
                {goal.isCompleted ? 'Meta concluída' : 'Aportar'}
            </button>

            {isContributeModalOpen && (
                <ContributeToGoalModal
                    goalId={goal.id}
                    onClose={() => setIsContributeModalOpen(false)}
                    onSuccess={() => {
                        setIsContributeModalOpen(false);
                        onContributionSuccess();
                    }}
                />
            )}
        </div>
    );
}
