import { useEffect, useState } from 'react';
import ReactDOM from 'react-dom';
import { accountService, categoryService, transactionService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { AccountSelectField } from '../Accounts/AccountSelectField';
import { TransactionType } from '../../types/TransactionType';
import './ContributeToGoalModal.css';

interface ContributeToGoalModalProps {
    goalId: string;
    onClose: () => void;
    onSuccess: () => void;
}

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

export function ContributeToGoalModal({ goalId, onClose, onSuccess }: ContributeToGoalModalProps) {
    const [accounts, setAccounts] = useState<AccountResponseDto[]>([]);
    const [selectedAccountId, setSelectedAccountId] = useState('');
    const [amount, setAmount] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [isLoadingData, setIsLoadingData] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const loadData = async () => {
            try {
                const [accountsRes, categoriesRes] = await Promise.all([
                    accountService.getAllAccounts(),
                    categoryService.getAll(),
                ]);
                setAccounts(accountsRes.data.sort((a, b) => a.name.localeCompare(b.name)));
                if (categoriesRes.data.length > 0) {
                    setCategoryId(categoriesRes.data[0].id);
                }
            } catch {
                setError('Não foi possível carregar os dados. Tente novamente.');
            } finally {
                setIsLoadingData(false);
            }
        };
        void loadData();
    }, []);

    const selectedAccount = accounts.find(a => a.id === selectedAccountId) ?? null;
    const parsedAmount = parseFloat(amount);
    const hasInsufficientBalance =
        selectedAccount !== null &&
        !isNaN(parsedAmount) &&
        parsedAmount > 0 &&
        parsedAmount > selectedAccount.currentBalance;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!selectedAccountId) { setError('Selecione uma conta de origem.'); return; }
        if (isNaN(parsedAmount) || parsedAmount <= 0) { setError('Insira um valor válido maior que zero.'); return; }
        if (!categoryId) { setError('Nenhuma categoria disponível. Crie uma categoria primeiro.'); return; }

        if (selectedAccount && parsedAmount > selectedAccount.currentBalance) {
            setError(
                `Saldo insuficiente. A conta "${selectedAccount.name}" possui apenas ${formatCurrency(selectedAccount.currentBalance)} disponível.`
            );
            return;
        }

        setIsLoading(true);
        setError(null);

        try {
            await transactionService.create({
                description: 'Aporte na meta',
                amount: parsedAmount,
                type: TransactionType.Investment,
                date: new Date().toISOString(),
                accountId: selectedAccountId,
                categoryId,
                financialGoalId: goalId,
            });
            onSuccess();
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Erro ao realizar o aporte. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    // Portal garante que o modal é renderizado em document.body,
    // escapando de qualquer stacking context criado por transform no card pai.
    return ReactDOM.createPortal(
        <div className="contribute-overlay" onClick={onClose}>
            <div className="contribute-modal" onClick={e => e.stopPropagation()}>
                <div className="contribute-modal-header">
                    <h2 className="contribute-modal-title">Realizar Aporte</h2>
                    <button className="contribute-modal-close" onClick={onClose} aria-label="Fechar">×</button>
                </div>

                {isLoadingData ? (
                    <p style={{ color: '#64748b', margin: '0.5rem 0' }}>Carregando...</p>
                ) : (
                    <form onSubmit={handleSubmit}>
                        {/* AccountSelectField usa classes do TransactionModal.css que já são globais */}
                        <AccountSelectField
                            accounts={accounts}
                            selectedId={selectedAccountId}
                            onChange={id => { setSelectedAccountId(id); setError(null); }}
                            onAccountCreated={newAcc =>
                                setAccounts(prev => [...prev, newAcc].sort((a, b) => a.name.localeCompare(b.name)))
                            }
                            allowCreation={false}
                            disabled={isLoading}
                        />

                        {/* Hint de saldo da conta selecionada */}
                        {selectedAccount && (
                            <p className={`contribute-balance-hint${hasInsufficientBalance ? ' insufficient' : ''}`}>
                                {hasInsufficientBalance
                                    ? `Saldo insuficiente — disponível: ${formatCurrency(selectedAccount.currentBalance)}`
                                    : `Saldo disponível: ${formatCurrency(selectedAccount.currentBalance)}`
                                }
                            </p>
                        )}

                        <div className="contribute-form-group" style={{ marginTop: '1rem' }}>
                            <label htmlFor="contributeAmount">Valor (R$)</label>
                            <input
                                id="contributeAmount"
                                type="number"
                                min="0.01"
                                step="0.01"
                                placeholder="0.00"
                                value={amount}
                                onChange={e => { setAmount(e.target.value); setError(null); }}
                                disabled={isLoading}
                                autoFocus
                            />
                        </div>

                        {error && <span className="contribute-error">{error}</span>}

                        <div className="contribute-actions">
                            <button
                                type="button"
                                className="contribute-btn-cancel"
                                onClick={onClose}
                                disabled={isLoading}
                            >
                                Cancelar
                            </button>
                            <button
                                type="submit"
                                className="contribute-btn-submit"
                                disabled={isLoading || hasInsufficientBalance}
                            >
                                {isLoading ? 'Enviando...' : 'Confirmar Aporte'}
                            </button>
                        </div>
                    </form>
                )}
            </div>
        </div>,
        document.body
    );
}
