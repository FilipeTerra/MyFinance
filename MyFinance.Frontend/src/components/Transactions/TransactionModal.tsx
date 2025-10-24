// src/components/Transactions/TransactionModal.tsx
import React, { useState } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import './TransactionModal.css';

interface TransactionModalProps {
    accounts: AccountResponseDto[];
    isOpen: boolean;
    onClose: () => void;
    // Adicione a função para criar a transação depois
}

export function TransactionModal({ accounts, isOpen, onClose }: TransactionModalProps) {
    const [description, setDescription] = useState('');
    const [amount, setAmount] = useState('');
    const [date, setDate] = useState(new Date().toISOString().split('T')[0]); // Data de hoje
    const [accountId, setAccountId] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [newCategory, setNewCategory] = useState('');

    if (!isOpen) {
        return null;
    }

    const handleCreateCategory = () => {
        // Lógica para criar a categoria
        console.log("Criando nova categoria:", newCategory);
        // Adicione a lógica para salvar a categoria e atualizar a lista
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content">
                <h2>Nova Transação</h2>
                <form>
                    <div className="form-group">
                        <label htmlFor="description">Descrição</label>
                        <input
                            type="text"
                            id="description"
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder="Ex: Almoço"
                        />
                    </div>
                    <div className="form-group">
                        <label htmlFor="amount">Valor</label>
                        <input
                            type="number"
                            id="amount"
                            value={amount}
                            onChange={(e) => setAmount(e.target.value)}
                            placeholder="Ex: 50.00"
                            step="0.01"
                        />
                    </div>
                    <div className="form-group">
                        <label htmlFor="date">Data</label>
                        <input
                            type="date"
                            id="date"
                            value={date}
                            onChange={(e) => setDate(e.target.value)}
                        />
                    </div>
                    <div className="form-group">
                        <label htmlFor="account">Conta</label>
                        <select id="account" value={accountId} onChange={(e) => setAccountId(e.target.value)}>
                            <option value="" disabled>Selecione uma conta</option>
                            {accounts.map(account => (
                                <option key={account.id} value={account.id}>
                                    {account.name}
                                </option>
                            ))}
                        </select>
                    </div>
                    <div className="form-group">
                        <label htmlFor="category">Categoria</label>
                        <select id="category" value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
                            <option value="" disabled>Selecione uma categoria</option>
                            {/* Adicione as categorias aqui */}
                        </select>
                    </div>
                    <div className="form-group new-category-group">
                        <input
                            type="text"
                            value={newCategory}
                            onChange={(e) => setNewCategory(e.target.value)}
                            placeholder="Ou crie uma nova categoria"
                        />
                        <button type="button" onClick={handleCreateCategory}>
                            Criar
                        </button>
                    </div>
                    <div className="modal-actions">
                        <button type="button" onClick={onClose} className="cancel-button">
                            Cancelar
                        </button>
                        <button type="submit" className="save-button">
                            Salvar
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}   