import { useEffect, useState } from 'react';
import { Header } from '../components/Layout/Header';
import { FinancialGoalCard } from '../components/FinancialGoals/FinancialGoalCard';
import { financialGoalService } from '../services/Api';
import type { FinancialGoalResponseDto } from '../types/FinancialGoalResponseDto';
import './DashboardPage.css';

export function DashboardPage() {
    const [goals, setGoals] = useState<FinancialGoalResponseDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const fetchGoals = async () => {
            try {
                const data = await financialGoalService.getAll();
                setGoals(data);
            } catch {
                setError('Não foi possível carregar as metas. Tente novamente mais tarde.');
            } finally {
                setIsLoading(false);
            }
        };

        void fetchGoals();
    }, []);

    return (
        <div className="dashboard-container">
            <Header />

            <main className="dashboard-content">
                <h2>Minhas Metas</h2>

                {error && <div className="error-message">{error}</div>}

                {isLoading ? (
                    <p className="dashboard-feedback">Carregando metas...</p>
                ) : goals.length === 0 ? (
                    <div className="dashboard-empty">
                        <p>Você ainda não tem nenhuma meta cadastrada.</p>
                        <p>
                            Converse com o <strong>Assistente IA</strong> e peça para criar sua primeira meta financeira!
                        </p>
                    </div>
                ) : (
                    <div className="goals-grid">
                        {goals.map(goal => (
                            <FinancialGoalCard key={goal.id} goal={goal} />
                        ))}
                    </div>
                )}
            </main>
        </div>
    );
}
