import { useEffect, useState } from 'react';
import { Modal } from '../Shared/ui/Modal';
import {
    investimentoService,
    accountService,
    categoryService,
    AxiosError,
    type ApiErrorResponse,
} from '../../services/Api';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { AccountSelectField } from '../Accounts/AccountSelectField';
import '../Transactions/TransactionModal.css';
import './InvestimentoModal.css';

interface AporteInvestimentoModalProps {
    investimentoId: string;
    investimentoNome: string;
    onClose: () => void;
    onSuccess: () => void;
}

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

function maskCurrency(raw: string): string {
    let digits = raw.replace(/\D/g, '');
    if (digits === '') return '';
    if (digits.length > 1) digits = digits.replace(/^0+/, '');
    while (digits.length < 3) digits = '0' + digits;
    const decimalIndex = digits.length - 2;
    const integerPart = digits.slice(0, decimalIndex);
    const decimalPart = digits.slice(decimalIndex);
    const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    return formattedInteger + ',' + decimalPart;
}

const parseCurrency = (masked: string): number =>
    parseFloat(masked.replace(/\./g, '').replace(',', '.'));

export function AporteInvestimentoModal({ investimentoId, investimentoNome, onClose, onSuccess }: AporteInvestimentoModalProps) {
    const [accounts, setAccounts] = useState<AccountResponseDto[]>([]);
    const [selectedAccountId, setSelectedAccountId] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [valor, setValor] = useState('');
    const [isLoadingData, setIsLoadingData] = useState(true);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const loadData = async () => {
            try {
                const [accountsRes, categoriesRes] = await Promise.all([
                    accountService.getAllAccounts(),
                    categoryService.getAll(),
                ]);
                setAccounts(accountsRes.data.sort((a, b) => a.name.localeCompare(b.name)));
                if (categoriesRes.data.length > 0) setCategoryId(categoriesRes.data[0].id);
            } catch {
                setError('Não foi possível carregar contas e categorias. Tente novamente.');
            } finally {
                setIsLoadingData(false);
            }
        };
        void loadData();
    }, []);

    const parsedValor = parseCurrency(valor);
    const selectedAccount = accounts.find(a => a.id === selectedAccountId) ?? null;
    const hasInsufficientBalance =
        selectedAccount !== null &&
        !isNaN(parsedValor) &&
        parsedValor > 0 &&
        parsedValor > selectedAccount.currentBalance;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (isNaN(parsedValor) || parsedValor <= 0) { setError('Insira um valor válido maior que zero.'); return; }
        if (!selectedAccountId) { setError('Selecione a conta de origem.'); return; }
        if (!categoryId) { setError('Nenhuma categoria disponível. Crie uma categoria primeiro.'); return; }
        if (selectedAccount && parsedValor > selectedAccount.currentBalance) {
            setError(
                `Saldo insuficiente. A conta "${selectedAccount.name}" possui apenas ${formatCurrency(selectedAccount.currentBalance)} disponível.`
            );
            return;
        }

        setIsLoading(true);
        setError(null);
        try {
            await investimentoService.adicionarAporte(investimentoId, {
                valor: parsedValor,
                accountId: selectedAccountId,
                categoryId,
            });
            onSuccess();
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Erro ao realizar o aporte. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <Modal
            onFechar={onClose}
            titulo="Aportar mais"
            tamanho="md"
        >

                <div className="inv-edit-context">
                    <span className="inv-edit-context-name">{investimentoNome}</span>
                </div>

                {isLoadingData ? (
                    <p style={{ color: '#64748b', margin: '0.5rem 0' }}>Carregando...</p>
                ) : (
                    <form onSubmit={handleSubmit}>
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
                        {selectedAccount && (
                            <p className={`inv-balance-hint${hasInsufficientBalance ? ' insufficient' : ''}`}>
                                {hasInsufficientBalance
                                    ? `Saldo insuficiente — disponível: ${formatCurrency(selectedAccount.currentBalance)}`
                                    : `Saldo disponível: ${formatCurrency(selectedAccount.currentBalance)}`}
                            </p>
                        )}

                        <div className="inv-form-group" style={{ marginTop: '1rem' }}>
                            <label htmlFor="aporteValor">Valor do aporte (R$)</label>
                            <input
                                id="aporteValor"
                                type="text"
                                inputMode="numeric"
                                placeholder="0,00"
                                value={valor}
                                onChange={e => { setValor(maskCurrency(e.target.value)); setError(null); }}
                                disabled={isLoading}
                                autoFocus
                            />
                        </div>

                        {error && <span className="inv-error">{error}</span>}

                        <div className="inv-actions">
                            <button type="button" className="inv-btn-cancel" onClick={onClose} disabled={isLoading}>
                                Cancelar
                            </button>
                            <button
                                type="submit"
                                className="inv-btn-submit"
                                disabled={isLoading || hasInsufficientBalance}
                            >
                                {isLoading ? 'Salvando...' : 'Confirmar aporte'}
                            </button>
                        </div>
                    </form>
                )}
        </Modal>
    );
}
