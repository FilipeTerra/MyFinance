import { useEffect, useState } from 'react';
import { Modal } from '../Shared/ui/Modal';
import {
    investimentoService,
    accountService,
    categoryService,
    AxiosError,
    type ApiErrorResponse,
} from '../../services/Api';
import { InvestmentType } from '../../types/InvestmentType';
import type { InvestimentoResponseDto } from '../../types/InvestimentoResponseDto';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { AccountSelectField } from '../Accounts/AccountSelectField';
import { INVESTMENT_TYPE_META } from './investmentTypeMeta';
// AccountSelectField usa classes (.input-wrapper, .add-new-button, .form-group-with-button)
// definidas em TransactionModal.css — importado aqui para não depender de ordem de bundle.
import '../Transactions/TransactionModal.css';
import './InvestimentoModal.css';

type ModalMode = 'create' | 'edit';

interface InvestimentoModalProps {
    mode: ModalMode;
    /** Obrigatório quando mode === 'edit'. */
    investimento?: InvestimentoResponseDto;
    onClose: () => void;
    onSuccess: () => void;
}

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

/** Máscara monetária: converte dígitos digitados em "1.234,56". */
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

const TYPE_OPTIONS = Object.values(InvestmentType);

export function InvestimentoModal({ mode, investimento, onClose, onSuccess }: InvestimentoModalProps) {
    const isEdit = mode === 'edit';

    const [nome, setNome] = useState('');
    const [tipo, setTipo] = useState<InvestmentType>(InvestmentType.RendaFixa);
    const [ticker, setTicker] = useState('');
    const [valor, setValor] = useState(
        isEdit && investimento ? maskCurrency(investimento.valorAtual.toFixed(2).replace('.', '')) : ''
    );

    // Origem do dinheiro (apenas no modo criação)
    const [accounts, setAccounts] = useState<AccountResponseDto[]>([]);
    const [selectedAccountId, setSelectedAccountId] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [isLoadingData, setIsLoadingData] = useState(!isEdit);

    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (isEdit) return;
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
    }, [isEdit]);

    const parsedValor = parseCurrency(valor);
    const selectedAccount = accounts.find(a => a.id === selectedAccountId) ?? null;
    const hasInsufficientBalance =
        !isEdit &&
        selectedAccount !== null &&
        !isNaN(parsedValor) &&
        parsedValor > 0 &&
        parsedValor > selectedAccount.currentBalance;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (isNaN(parsedValor) || parsedValor <= 0) {
            setError('Insira um valor válido maior que zero.');
            return;
        }

        if (isEdit && investimento) {
            setIsLoading(true);
            setError(null);
            try {
                await investimentoService.updateValorAtual(investimento.id, parsedValor);
                onSuccess();
            } catch (err) {
                const axiosError = err as AxiosError<ApiErrorResponse>;
                setError(axiosError.response?.data?.message || 'Erro ao salvar. Tente novamente.');
            } finally {
                setIsLoading(false);
            }
            return;
        }

        // Modo criação — valida origem do dinheiro
        if (!nome.trim()) { setError('Informe o nome do investimento.'); return; }
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
            await investimentoService.create({
                nome: nome.trim(),
                tipo,
                valorInicial: parsedValor,
                accountId: selectedAccountId,
                categoryId,
                ticker: tipo !== InvestmentType.RendaFixa && ticker.trim() ? ticker.trim().toUpperCase() : undefined,
            });
            onSuccess();
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Erro ao salvar. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <Modal
            onFechar={onClose}
            titulo={isEdit ? 'Atualizar Valor de Mercado' : 'Novo Investimento'}
            tamanho="md"
        >

                {isEdit && investimento && (
                    <div className="inv-edit-context">
                        <span className="inv-edit-context-name">{investimento.nome}</span>
                        <span className="inv-edit-context-sub">
                            Total aportado: {formatCurrency(investimento.totalAportado)}
                        </span>
                    </div>
                )}

                {!isEdit && isLoadingData ? (
                    <p style={{ color: '#64748b', margin: '0.5rem 0' }}>Carregando...</p>
                ) : (
                    <form onSubmit={handleSubmit}>
                        {!isEdit && (
                            <>
                                <div className="inv-form-group">
                                    <label htmlFor="invNome">Nome do ativo</label>
                                    <input
                                        id="invNome"
                                        type="text"
                                        placeholder="Ex: Tesouro Selic 2029, PETR4, HGLG11"
                                        value={nome}
                                        onChange={e => { setNome(e.target.value); setError(null); }}
                                        disabled={isLoading}
                                        autoFocus
                                        maxLength={80}
                                    />
                                </div>

                                <div className="inv-form-group">
                                    <label>Classe do ativo</label>
                                    <div className="inv-type-grid" role="radiogroup" aria-label="Classe do ativo">
                                        {TYPE_OPTIONS.map(t => {
                                            const meta = INVESTMENT_TYPE_META[t];
                                            const active = tipo === t;
                                            return (
                                                <button
                                                    type="button"
                                                    key={t}
                                                    role="radio"
                                                    aria-checked={active}
                                                    className={`inv-type-chip${active ? ' inv-type-chip--active' : ''}`}
                                                    style={active ? { '--chip-color': meta.color } as React.CSSProperties : undefined}
                                                    onClick={() => setTipo(t)}
                                                    disabled={isLoading}
                                                >
                                                    <span className="inv-type-chip-icon" aria-hidden="true">{meta.icon}</span>
                                                    {meta.label}
                                                </button>
                                            );
                                        })}
                                    </div>
                                </div>

                                {tipo !== InvestmentType.RendaFixa && (
                                    <div className="inv-form-group">
                                        <label htmlFor="invTicker">Ticker na B3 (opcional)</label>
                                        <input
                                            id="invTicker"
                                            type="text"
                                            placeholder="Ex: PETR4"
                                            value={ticker}
                                            onChange={e => setTicker(e.target.value)}
                                            disabled={isLoading}
                                            maxLength={10}
                                        />
                                        <p className="inv-ticker-hint">
                                            Informe o ticker para que a cotação seja atualizada automaticamente.
                                        </p>
                                    </div>
                                )}

                                {/* Origem do dinheiro — conta a ser debitada */}
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
                            </>
                        )}

                        <div className="inv-form-group" style={!isEdit ? { marginTop: '1rem' } : undefined}>
                            <label htmlFor="invValor">{isEdit ? 'Valor atual (R$)' : 'Valor a investir (R$)'}</label>
                            <input
                                id="invValor"
                                type="text"
                                inputMode="numeric"
                                placeholder="0,00"
                                value={valor}
                                onChange={e => { setValor(maskCurrency(e.target.value)); setError(null); }}
                                disabled={isLoading}
                                autoFocus={isEdit}
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
                                {isLoading ? 'Salvando...' : isEdit ? 'Atualizar' : 'Adicionar'}
                            </button>
                        </div>
                    </form>
                )}
        </Modal>
    );
}
