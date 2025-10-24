import React from 'react';
import type { TransactionResponseDto } from '../../types/TransactionResponseDto';
import './TransactionList.css'; // Criaremos este CSS

interface TransactionListProps {
    transactions: TransactionResponseDto[];
    isLoading: boolean;
}

export function TransactionList({ transactions, isLoading }: TransactionListProps) {

    if (isLoading) {
        return <div className="list-message">Carregando transações...</div>;
    }

    if (transactions.length === 0) {
        return <div className="list-message">Nenhuma transação encontrada para este filtro.</div>;
    }

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('pt-BR', { timeZone: 'UTC' });
    };

    return (
        <div className="transaction-list-container">
            <h3>Transações e contas</h3>
            <table className="transaction-table">
                <thead>
                    <tr>
                        <th>Data</th>
                        <th>Descrição</th>
                        <th>Conta</th>
                        <th>Tipo</th>
                        <th>Valor</th>
                        <th>Ações</th>
                    </tr>
                </thead>
                <tbody>
                    {transactions.map(tx => (
                        <tr key={tx.id}>
                            <td>{formatDate(tx.date)}</td>
                            <td>{tx.description}</td>
                            <td>{tx.accountName}</td>
                            <td>
                                <span className={`tx-type ${tx.typeName.toLowerCase()}`}>
                                    {tx.typeName === 'Income' ? 'Receita' : 'Despesa'}
                                </span>
                            </td>
                            <td className={`tx-amount ${tx.typeName.toLowerCase()}`}>
                                {tx.typeName === 'Expense' ? '- ' : '+ '}
                                {tx.amount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                            </td>
                            <td className="tx-actions">
                                <button className="action-btn edit-btn">Editar</button>
                                <button className="action-btn delete-btn">Excluir</button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}