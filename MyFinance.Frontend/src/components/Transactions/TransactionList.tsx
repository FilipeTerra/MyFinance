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

            {/* Os `role` explícitos não são redundantes: no celular o CSS troca o
                `display` de table/tr/td para virar cartões, e isso apaga a
                semântica implícita de tabela. Sem eles, um leitor de tela
                perderia a relação entre cada valor e o cabeçalho da coluna. */}
            <table className="transaction-table" role="table">
                <thead className="transaction-table-head" role="rowgroup">
                    <tr role="row">
                        <th role="columnheader" scope="col">Data</th>
                        <th role="columnheader" scope="col">Descrição</th>
                        <th role="columnheader" scope="col">Categoria</th>
                        <th role="columnheader" scope="col">Tipo</th>
                        <th role="columnheader" scope="col">Valor</th>
                        <th role="columnheader" scope="col">Ações</th>
                    </tr>
                </thead>
                <tbody role="rowgroup">
                    {paginated.map(tx => (
                        <tr key={tx.id} role="row">
                            <td role="cell" className="tx-data">{formatDate(tx.date)}</td>
                            <td role="cell" className="tx-descricao">{tx.description}</td>
                            <td role="cell" className="tx-categoria">{tx.categoryName}</td>
                            <td role="cell">
                                <span className={`tx-type ${tx.typeName.toLowerCase()}`}>
                                    {tx.typeName === 'Income' ? 'Receita' : tx.typeName === 'Expense' ? 'Despesa' : 'Investimento'    }
                                </span>
                            </td>
                            <td role="cell" className={`tx-amount ${tx.typeName.toLowerCase()}`}>
                                {tx.typeName === 'Income' ? '+ ' : '- '}
                                {Math.abs(tx.amount).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                            </td>
                            <td role="cell" className="tx-actions">
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
                        aria-label="Página anterior"
                    >‹</button>
                    {/* No celular os números dão lugar a este resumo: com 500
                        transações são 20 botões numa linha só, que estouravam a
                        largura da tela. O CSS decide qual dos dois aparece. */}
                    <span className="pagination-resumo">
                        Página {currentPage} de {totalPages}
                    </span>
                    {pageNumbers.map(page => (
                        <button
                            key={page}
                            className={`pagination-btn pagination-numero${currentPage === page ? ' active' : ''}`}
                            onClick={() => setCurrentPage(page)}
                            aria-label={`Página ${page}`}
                            aria-current={currentPage === page ? 'page' : undefined}
                        >{page}</button>
                    ))}
                    <button
                        className="pagination-btn"
                        onClick={() => setCurrentPage(p => p + 1)}
                        disabled={currentPage === totalPages}
                        aria-label="Próxima página"
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