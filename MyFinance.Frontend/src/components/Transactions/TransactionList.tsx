import { useState } from 'react';
import type { TransactionResponseDto } from '../../types/TransactionResponseDto';
import './TransactionList.css';
// Importe o novo componente (ajuste o caminho conforme onde você salvou)
import { ConfirmationModal } from '../Shared/ConfirmationModal'; 

interface TransactionListProps {
    transactions: TransactionResponseDto[];
    isLoading: boolean;
    onDelete: (id: string) => Promise<void> | void;
}

export function TransactionList({ transactions, isLoading, onDelete }: TransactionListProps) {

    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [idToDelete, setIdToDelete] = useState<string | null>(null);

    const openDeleteModal = (id: string) => {
        setIdToDelete(id);
        setIsDeleteModalOpen(true);
    };

    const closeDeleteModal = () => {
        setIsDeleteModalOpen(false);
        setIdToDelete(null);
    };

    const confirmDelete = () => {
        if (idToDelete !== null) {
            onDelete(idToDelete);
            closeDeleteModal();
        }
    };

    if (isLoading) return <div className="list-message">Carregando transações...</div>;
    if (transactions.length === 0) return <div className="list-message">Nenhuma transação encontrada.</div>;

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('pt-BR', { timeZone: 'UTC' });
    };

    return (
        <div className="transaction-list-container">
            <h3>Transações e contas</h3>
            
            {/* Tabela de Transações */}
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
                                <button 
                                    className="action-btn delete-btn"
                                    onClick={() => openDeleteModal(tx.id)}
                                >
                                    Excluir
                                </button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            <ConfirmationModal 
                isOpen={isDeleteModalOpen}
                onClose={closeDeleteModal}
                onConfirm={confirmDelete}
                title="Excluir transação"
                description="Tem certeza de que deseja excluir esta transação? Esta ação não pode ser desfeita."
                confirmText="Sim, excluir"
                cancelText="Cancelar"
            />
        </div>
    );
}