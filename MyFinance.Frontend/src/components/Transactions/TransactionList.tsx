import { useState, useEffect } from 'react';
import type { TransactionResponseDto } from '../../types/TransactionResponseDto';
import './TransactionList.css';
import { ConfirmationModal } from '../Shared/ConfirmationModal';

const PAGE_SIZE = 25;

interface TransactionListProps {
    transactions: TransactionResponseDto[];
    isLoading: boolean;
    onDelete: (id: string) => Promise<void> | void;
    onEdit: (transaction: TransactionResponseDto) => void;
}

export function TransactionList({ transactions, isLoading, onDelete, onEdit }: TransactionListProps) {

    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [idToDelete, setIdToDelete] = useState<string | null>(null);
    const [currentPage, setCurrentPage] = useState(1);

    // Volta para a primeira página sempre que a lista mudar (filtro, nova importação, etc.)
    useEffect(() => {
        setCurrentPage(1);
    }, [transactions]);

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
    if (transactions.length === 0) return <div className="list-message">Nenhuma transação encontrada</div>;

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('pt-BR', { timeZone: 'UTC' });
    };

    const totalPages = Math.ceil(transactions.length / PAGE_SIZE);
    const paginated = transactions.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

    const pageNumbers = Array.from({ length: totalPages }, (_, i) => i + 1);

    return (
        <div className="transaction-list-container">
            <h3>Transações</h3>

            <table className="transaction-table">
                <thead>
                    <tr>
                        <th>Data</th>
                        <th>Descrição</th>
                        <th>Categoria</th>
                        <th>Tipo</th>
                        <th>Valor</th>
                        <th>Ações</th>
                    </tr>
                </thead>
                <tbody>
                    {paginated.map(tx => (
                        <tr key={tx.id}>
                            <td>{formatDate(tx.date)}</td>
                            <td>{tx.description}</td>
                            <td>{tx.categoryName}</td>
                            <td>
                                <span className={`tx-type ${tx.typeName.toLowerCase()}`}>
                                    {tx.typeName === 'Income' ? 'Receita' : tx.typeName === 'Expense' ? 'Despesa' : 'Investimento'    }
                                </span>
                            </td>
                            <td className={`tx-amount ${tx.typeName.toLowerCase()}`}>
                                {tx.typeName === 'Income' ? '+ ' : '- '}
                                {Math.abs(tx.amount).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                            </td>
                            <td className="tx-actions">
                                <button
                                    className="action-btn edit-btn"
                                    onClick={() => onEdit(tx)}
                                >Editar
                                </button>
                                <button
                                    className="action-btn delete-btn"
                                    onClick={() => openDeleteModal(tx.id)}
                                >Excluir
                                </button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            {totalPages > 1 && (
                <div className="pagination">
                    <span className="pagination-info">
                        {(currentPage - 1) * PAGE_SIZE + 1}–{Math.min(currentPage * PAGE_SIZE, transactions.length)} de {transactions.length}
                    </span>
                    <button
                        className="pagination-btn"
                        onClick={() => setCurrentPage(p => p - 1)}
                        disabled={currentPage === 1}
                    >‹</button>
                    {pageNumbers.map(page => (
                        <button
                            key={page}
                            className={`pagination-btn${currentPage === page ? ' active' : ''}`}
                            onClick={() => setCurrentPage(page)}
                        >{page}</button>
                    ))}
                    <button
                        className="pagination-btn"
                        onClick={() => setCurrentPage(p => p + 1)}
                        disabled={currentPage === totalPages}
                    >›</button>
                </div>
            )}

            <ConfirmationModal
                isOpen={isDeleteModalOpen}
                onClose={closeDeleteModal}
                onConfirm={confirmDelete}
                title="Excluir transação"
                description="Tem certeza de que deseja excluir esta transação? Esta ação não pode ser desfeita."
                confirmText="Excluir"
                cancelText="Cancelar"
            />
        </div>
    );
}