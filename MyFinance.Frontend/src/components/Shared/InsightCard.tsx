import { useState } from 'react';
import { financialGoalService } from '../../services/Api';
import './InsightCard.css';

interface InsightCardProps {
    curiosity: string;
    information: string;
    suggestion: string;
    hasAdequateReserve: boolean;
    alreadyHasReserveGoal: boolean;
    idealAmount: number;
    percentAchieved: number;
    onDismiss: () => void;
    onGoalCreated: () => void;
}

const RESERVE_GOAL_NAME = 'Reserva de Emergência';

function oneYearFromNow(): string {
    const date = new Date();
    date.setFullYear(date.getFullYear() + 1);
    return date.toISOString();
}

export function InsightCard({
    curiosity,
    information,
    suggestion,
    hasAdequateReserve,
    alreadyHasReserveGoal,
    idealAmount,
    percentAchieved,
    onDismiss,
    onGoalCreated,
}: InsightCardProps) {
    const [isCreating, setCreating] = useState(false);
    const [created, setCreated] = useState(false);
    const [createError, setCreateError] = useState<string | null>(null);

    const showButton = !hasAdequateReserve && !alreadyHasReserveGoal && !created;
    const progress = Math.min(100, Math.max(0, percentAchieved));

    const handleCreateGoal = async () => {
        setCreating(true);
        setCreateError(null);
        try {
            await financialGoalService.create({
                name: RESERVE_GOAL_NAME,
                targetAmount: idealAmount,
                deadline: oneYearFromNow(),
            });
            setCreated(true);
            onGoalCreated();
        } catch {
            setCreateError('Não foi possível criar a meta. Tente novamente.');
        } finally {
            setCreating(false);
        }
    };

    return (
        <div className="insight-card" role="status">
            <span className="insight-card-icon" aria-hidden="true">🛡️</span>

            <div className="insight-card-body">
                <span className="insight-card-title">Reserva de emergência</span>
                <p className="insight-card-curiosity">💡 {curiosity}</p>
                <p className="insight-card-text">{information}</p>

                {!hasAdequateReserve && (
                    <div className="insight-card-progress" aria-hidden="true">
                        <div className="insight-card-progress-fill" style={{ width: `${progress}%` }} />
                    </div>
                )}

                <p className="insight-card-suggestion">{suggestion}</p>

                {showButton && (
                    <button className="insight-card-cta" onClick={handleCreateGoal} disabled={isCreating}>
                        {isCreating ? 'Criando...' : 'Criar meta agora'}
                    </button>
                )}
                {created && (
                    <span className="insight-card-success">✅ Meta criada! Confira em Metas.</span>
                )}
                {createError && <span className="insight-card-error">{createError}</span>}
            </div>

            <button className="insight-card-close" onClick={onDismiss} aria-label="Dispensar insight">
                ×
            </button>
        </div>
    );
}
