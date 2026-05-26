import React, { useEffect, useState } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import './TransactionFilter.css'; // Criaremos este CSS

interface TransactionFilterProps {
    accounts: AccountResponseDto[]; // Lista de contas para o dropdown
    selectedAccountId?: string;
    onFilterChange: (filters: any) => void; // Função para enviar filtros para o pai
    isLoading: boolean;
}

export function TransactionFilter({ accounts, selectedAccountId = '', onFilterChange, isLoading }: TransactionFilterProps) {
    // Estado inicial dos filtros
    const [selectedAccount, setSelectedAccount] = useState<string>(selectedAccountId);
    const [searchText, setSearchText] = useState('');
    const [startDate, setStartDate] = useState('');
    const [endDate, setEndDate] = useState('');
    const [amount, setAmount] = useState('');
    const [transactionType, setTransactionType] = useState('');

    const handleFilterSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        onFilterChange({
            accountId: selectedAccount,
            searchText: searchText || undefined,
            startDate: startDate || undefined,
            endDate: endDate || undefined,
            amount: amount ? parseFloat(amount) : undefined,
            type: transactionType ? parseInt(transactionType) : undefined,
        });
    };

    const handleClearFilters = () => {
        setSearchText('');
        setStartDate('');
        setEndDate('');
        setAmount('');
        setTransactionType('');
        onFilterChange({ accountId: selectedAccount });
    };

    const hasActiveFilters = searchText || startDate || endDate || amount || transactionType;

    useEffect(() => {
        setSelectedAccount(selectedAccountId);
    }, [selectedAccountId]);

    return (
        <form className="transaction-filter-form" onSubmit={handleFilterSubmit}>
            <h4>Filtrar Transações</h4>

            {/* Filtro de Conta (Obrigatório por enquanto) */}
            <div className="filter-group">
                <label htmlFor="account-filter">Conta</label>
                <select
                    id="account-filter"
                    value={selectedAccount}
                    onChange={(e) => setSelectedAccount(e.target.value)}
                    required // Força o usuário a escolher uma conta
                >
                    <option value="" disabled>Selecione uma conta...</option>
                    {accounts.map(account => (
                        <option key={account.id} value={account.id}>
                            {account.name}
                        </option>
                    ))}
                </select>
            </div>

            {/* Filtro de Texto */}
            <div className="filter-group">
                <label htmlFor="text-filter">Descrição</label>
                <input
                    type="text"
                    id="text-filter"
                    placeholder="Ex: Almoço"
                    value={searchText}
                    onChange={(e) => setSearchText(e.target.value)}
                />
            </div>

            {/* Filtro de Período */}
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
                    <span>até</span>
                    <input
                        type="date"
                        id="end-date-filter"
                        value={endDate}
                        min={startDate || undefined}
                        onChange={(e) => setEndDate(e.target.value)}
                    />
                </div>
            </div>

            {/* Filtro de Valor */}
            <div className="filter-group">
                <label htmlFor="amount-filter">Valor</label>
                <input
                    type="number"
                    id="amount-filter"
                    placeholder="Ex: 50.00"
                    step="0.01"
                    value={amount}
                    onChange={(e) => setAmount(e.target.value)}
                />
            </div>

            {/* Filtro de Tipo */}
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

            <button type="submit" className="filter-button" disabled={isLoading || !selectedAccount}>
                {isLoading ? 'Buscando...' : 'Buscar'}
            </button>

            {hasActiveFilters && (
                <button type="button" className="filter-button clear-button" onClick={handleClearFilters}>
                    Limpar
                </button>
            )}
        </form>
    );
}