import React, { useEffect, useState } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import type { CategoryResponseDto } from '../../types/CategoryResponseDto';
import './TransactionFilter.css';

interface TransactionFilterProps {
    accounts: AccountResponseDto[];
    categories: CategoryResponseDto[];
    selectedAccountId?: string;
    onFilterChange: (filters: any) => void;
    isLoading: boolean;
}

interface Chip {
    key: string;
    label: string;
    onRemove: () => void;
}

function formatDateBR(iso: string): string {
    const [y, m, d] = iso.split('-');
    return `${d}/${m}/${y}`;
}

export function TransactionFilter({ accounts, categories, selectedAccountId = '', onFilterChange, isLoading }: TransactionFilterProps) {
    const [isOpen, setIsOpen] = useState(true);
    const [selectedAccount, setSelectedAccount] = useState<string>(selectedAccountId);
    const [searchText, setSearchText] = useState('');
    const [startDate, setStartDate] = useState('');
    const [endDate, setEndDate] = useState('');
    const [amount, setAmount] = useState('');
    const [transactionType, setTransactionType] = useState('');
    const [categoryId, setCategoryId] = useState('');

    useEffect(() => {
        setSelectedAccount(selectedAccountId);
    }, [selectedAccountId]);

    // Builds the filter object from current state, with optional field overrides.
    // Overrides are used by chip removal to bypass stale state before React re-renders.
    const buildFilters = (overrides: Record<string, any> = {}) => ({
        accountId: selectedAccount,
        searchText: searchText || undefined,
        startDate: startDate || undefined,
        endDate: endDate || undefined,
        amount: amount ? parseFloat(amount) : undefined,
        type: transactionType ? parseInt(transactionType) : undefined,
        categoryId: categoryId || undefined,
        ...overrides,
    });

    const handleFilterSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        onFilterChange(buildFilters());
    };

    const handleClearAll = () => {
        setSearchText('');
        setStartDate('');
        setEndDate('');
        setAmount('');
        setTransactionType('');
        setCategoryId('');
        onFilterChange({ accountId: selectedAccount });
    };

    const chips: Chip[] = [
        searchText ? {
            key: 'search',
            label: `"${searchText}"`,
            onRemove: () => { setSearchText(''); onFilterChange(buildFilters({ searchText: undefined })); },
        } : null,
        startDate && endDate ? {
            key: 'period',
            label: `${formatDateBR(startDate)} → ${formatDateBR(endDate)}`,
            onRemove: () => { setStartDate(''); setEndDate(''); onFilterChange(buildFilters({ startDate: undefined, endDate: undefined })); },
        } : null,
        startDate && !endDate ? {
            key: 'startDate',
            label: `A partir de ${formatDateBR(startDate)}`,
            onRemove: () => { setStartDate(''); onFilterChange(buildFilters({ startDate: undefined })); },
        } : null,
        !startDate && endDate ? {
            key: 'endDate',
            label: `Até ${formatDateBR(endDate)}`,
            onRemove: () => { setEndDate(''); onFilterChange(buildFilters({ endDate: undefined })); },
        } : null,
        amount ? {
            key: 'amount',
            label: `R$ ${parseFloat(amount).toFixed(2).replace('.', ',')}`,
            onRemove: () => { setAmount(''); onFilterChange(buildFilters({ amount: undefined })); },
        } : null,
        transactionType ? {
            key: 'type',
            label: transactionType === '1' ? 'Receitas' : 'Despesas',
            onRemove: () => { setTransactionType(''); onFilterChange(buildFilters({ type: undefined })); },
        } : null,
        categoryId ? {
            key: 'category',
            label: categories.find(c => c.id === categoryId)?.name ?? 'Categoria',
            onRemove: () => { setCategoryId(''); onFilterChange(buildFilters({ categoryId: undefined })); },
        } : null,
    ].filter(Boolean) as Chip[];

    return (
        <div className="transaction-filter">

            {/* Toggle header */}
            <button
                type="button"
                className="filter-header"
                onClick={() => setIsOpen(prev => !prev)}
                aria-expanded={isOpen}
            >
                <div className="filter-header-left">
                    <svg className="filter-icon" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                        <line x1="4" y1="6" x2="20" y2="6" />
                        <line x1="8" y1="12" x2="16" y2="12" />
                        <line x1="11" y1="18" x2="13" y2="18" />
                    </svg>
                    <span className="filter-title">Filtrar Transações</span>
                    {chips.length > 0 && (
                        <span className="filter-badge">{chips.length}</span>
                    )}
                </div>
                <svg
                    className={`filter-chevron ${isOpen ? 'open' : ''}`}
                    width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"
                >
                    <polyline points="6 9 12 15 18 9" />
                </svg>
            </button>

            {/* Active filter chips — always visible when present */}
            {chips.length > 0 && (
                <div className="filter-chips">
                    {chips.map(chip => (
                        <span key={chip.key} className="filter-chip">
                            {chip.label}
                            <button
                                type="button"
                                className="filter-chip-remove"
                                onClick={chip.onRemove}
                                aria-label={`Remover filtro ${chip.label}`}
                            >
                                ×
                            </button>
                        </span>
                    ))}
                </div>
            )}

            {/* Collapsible form body */}
            <div className={`filter-body ${isOpen ? 'open' : ''}`}>
                {chips.length > 0 && <div className="filter-divider" />}
                <form onSubmit={handleFilterSubmit}>
                    <div className="filter-grid">

                        {/* Conta */}
                        <div className="filter-group">
                            <label htmlFor="account-filter">Conta</label>
                            <select
                                id="account-filter"
                                value={selectedAccount}
                                onChange={(e) => setSelectedAccount(e.target.value)}
                                required
                            >
                                <option value="" disabled>Selecione...</option>
                                {accounts.map(a => (
                                    <option key={a.id} value={a.id}>{a.name}</option>
                                ))}
                            </select>
                        </div>

                        {/* Descrição */}
                        <div className="filter-group">
                            <label htmlFor="text-filter">Descrição</label>
                            <div className="input-with-icon">
                                <svg className="input-icon" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                    <circle cx="11" cy="11" r="8" />
                                    <line x1="21" y1="21" x2="16.65" y2="16.65" />
                                </svg>
                                <input
                                    type="text"
                                    id="text-filter"
                                    placeholder="Ex: Almoço"
                                    value={searchText}
                                    onChange={(e) => setSearchText(e.target.value)}
                                />
                            </div>
                        </div>

                        {/* Período */}
                        <div className="filter-group date-range">
                            <label>Período</label>
                            <div className="date-range-inputs">
                                <input
                                    type="date"
                                    id="start-date-filter"
                                    value={startDate}
                                    max={endDate || undefined}
                                    onChange={(e) => setStartDate(e.target.value)}
                                />
                                <span className="date-sep">→</span>
                                <input
                                    type="date"
                                    id="end-date-filter"
                                    value={endDate}
                                    min={startDate || undefined}
                                    onChange={(e) => setEndDate(e.target.value)}
                                />
                            </div>
                        </div>

                        {/* Valor */}
                        <div className="filter-group">
                            <label htmlFor="amount-filter">Valor</label>
                            <input
                                type="number"
                                id="amount-filter"
                                placeholder="Ex: 50,00"
                                step="0.01"
                                min="0"
                                value={amount}
                                onChange={(e) => setAmount(e.target.value)}
                            />
                        </div>

                        {/* Tipo */}
                        <div className="filter-group">
                            <label htmlFor="type-filter">Tipo</label>
                            <select
                                id="type-filter"
                                value={transactionType}
                                onChange={(e) => setTransactionType(e.target.value)}
                            >
                                <option value="">Todos</option>
                                <option value="1">Receita</option>
                                <option value="2">Despesa</option>
                            </select>
                        </div>

                        {/* Categoria */}
                        <div className="filter-group">
                            <label htmlFor="category-filter">Categoria</label>
                            <select
                                id="category-filter"
                                value={categoryId}
                                onChange={(e) => setCategoryId(e.target.value)}
                            >
                                <option value="">Todas</option>
                                {categories.map(c => (
                                    <option key={c.id} value={c.id}>{c.name}</option>
                                ))}
                            </select>
                        </div>

                    </div>

                    <div className="filter-actions">
                        <button
                            type="button"
                            className="btn-filter-clear"
                            onClick={handleClearAll}
                            disabled={chips.length === 0}
                        >
                            Limpar
                        </button>
                        <button
                            type="submit"
                            className="btn-filter-submit"
                            disabled={isLoading || !selectedAccount}
                        >
                            {isLoading ? (
                                <>
                                    <span className="btn-spinner" />
                                    Buscando...
                                </>
                            ) : (
                                <>
                                    Buscar
                                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                        <line x1="5" y1="12" x2="19" y2="12" />
                                        <polyline points="12 5 19 12 12 19" />
                                    </svg>
                                </>
                            )}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}
