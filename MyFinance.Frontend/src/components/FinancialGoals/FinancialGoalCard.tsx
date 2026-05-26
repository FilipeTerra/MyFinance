import type { FinancialGoalResponseDto } from '../../types/FinancialGoalResponseDto';
import './FinancialGoalCard.css';

interface FinancialGoalCardProps {
    goal: FinancialGoalResponseDto;
}

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

const formatDate = (isoString: string) =>
    new Intl.DateTimeFormat('pt-BR').format(new Date(isoString));

export function FinancialGoalCard({ goal }: FinancialGoalCardProps) {
    const progress = Math.min((goal.currentAmount / goal.targetAmount) * 100, 100);

    return (
        <div className={`goal-card ${goal.isCompleted ? 'goal-card--completed' : ''}`}>
            <div className="goal-card-header">
                <h3 className="goal-card-name">{goal.name}</h3>
                {goal.isCompleted && <span className="goal-badge">Concluída ✓</span>}
            </div>

            <div className="goal-progress-track">
                <div className="goal-progress-fill" style={{ width: `${progress}%` }} />
            </div>

            <div className="goal-progress-label">
                <span>{formatCurrency(goal.currentAmount)}</span>
                <span className="goal-progress-pct">{progress.toFixed(0)}%</span>
                <span>{formatCurrency(goal.targetAmount)}</span>
            </div>

            <p className="goal-deadline">Prazo: {formatDate(goal.deadline)}</p>
        </div>
    );
}
