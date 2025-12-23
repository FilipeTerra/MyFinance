import React, { useState } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import './TransactionFilter.css'; // Criaremos este CSS

interface TransactionFilterProps {
    accounts: AccountResponseDto[]; // Lista de contas para o dropdown
    onFilterChange: (filters: any) => void; // Função para enviar filtros para o pai
    isLoading: boolean;
}

export function TransactionFilter({ accounts, onFilterChange, isLoading }: TransactionFilterProps) {
    // Estado inicial dos filtros
    const [selectedAccountId, setSelectedAccountId] = useState<string>('');
    const [searchText, setSearchText] = useState('');
    const [date, setDate] = useState('');
    const [amount, setAmount] = useState('');

    const handleFilterSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        onFilterChange({
            accountId: selectedAccountId,
            searchText: searchText || undefined,
            date: date || undefined,
            amount: amount ? parseFloat(amount) : undefined,
        });
    };

    return (
        <form className="transaction-filter-form" onSubmit={handleFilterSubmit}>
            <h4>Filtrar Transações</h4>

            {/* Filtro de Conta (Obrigatório por enquanto) */}
            <div className="filter-group">
                <label htmlFor="account-filter">Conta</label>
                <select
                    id="account-filter"
                    value={selectedAccountId}
                    onChange={(e) => setSelectedAccountId(e.target.value)}
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

            {/* Filtro de Data */}
            <div className="filter-group">
                <label htmlFor="date-filter">Data</label>
                <input
                    type="date"
                    id="date-filter"
                    value={date}
                    onChange={(e) => setDate(e.target.value)}
                />
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

            <button type="submit" className="filter-button" disabled={isLoading || !selectedAccountId}>
                {isLoading ? 'Buscando...' : 'Buscar'}
            </button>
        </form>
    );
}