// src/components/Transactions/ReviewImportModal.tsx
import React, { useState, useEffect } from 'react';
import type { AiTransactionResponseDto, SaveBatchTransactionRequestDto } from '../../types/AiIntegration';
import type { CategoryResponseDto } from '../../types/CategoryResponseDto';
import './ReviewImportModal.css';

interface ReviewImportModalProps {
    isOpen: boolean;
    onClose: () => void;
    aiTransactions: AiTransactionResponseDto[];
    categories: CategoryResponseDto[];
    onConfirm: (finalTransactions: SaveBatchTransactionRequestDto[]) => void;
}

export const ReviewImportModal: React.FC<ReviewImportModalProps> = ({
    isOpen,
    onClose,
    aiTransactions,
    categories,
    onConfirm
}) => {
    const [editableTransactions, setEditableTransactions] = useState<SaveBatchTransactionRequestDto[]>([]);

    useEffect(() => {
        if (aiTransactions && aiTransactions.length > 0) {
            const initialData: SaveBatchTransactionRequestDto[] = aiTransactions.map(tx => ({
                date: tx.date,
                description: tx.description,
                amount: tx.amount,
                accountId: tx.accountId,
                categoryId: tx.categoryId,
                newCategoryName: tx.isSuggestion ? tx.suggestedCategoryName : null,
                isNewCategory: tx.isSuggestion
            }));
            setEditableTransactions(initialData);
        }
    }, [aiTransactions]);

    const handleCategoryChange = (index: number, selectedValue: string, originalSuggestion: string | null) => {
        const updatedList = [...editableTransactions];
        
        if (selectedValue === "SUGGESTION") {
            updatedList[index].isNewCategory = true;
            updatedList[index].categoryId = null;
            updatedList[index].newCategoryName = originalSuggestion;
        } else {
            updatedList[index].isNewCategory = false;
            updatedList[index].categoryId = selectedValue;
            updatedList[index].newCategoryName = null;
        }
        
        setEditableTransactions(updatedList);
    };

    const canSubmit = editableTransactions.every(tx => tx.isNewCategory || tx.categoryId);

    if (!isOpen) return null;

    return (
        <div className="modal-overlay">
            {/* 1. Adicionamos a classe review-modal-content aqui */}
            <div className="modal-content review-modal-content">
                <div className="modal-header">
                    <h2>Revisão de Inteligência Artificial</h2>
                    <button className="close-button" onClick={onClose}>&times;</button>
                </div>
                
                <div className="modal-body">
                    <p>Revise as classificações sugeridas pela IA. Altere a categoria caso a IA tenha cometido um erro.</p>
                    
                    {/* 2. Trocamos o style inline pela classe table-container */}
                    <div className="table-container">
                        <table className="transactions-table">
                            <thead>
                                <tr>
                                    <th>Data</th>
                                    <th>Descrição</th>
                                    <th>Valor</th>
                                    <th>Categoria</th>
                                    <th>Status</th>
                                </tr>
                            </thead>
                            <tbody>
                                {editableTransactions.map((tx, index) => {
                                    const originalAiTx = aiTransactions[index]; 
                                    
                                    return (
                                        <tr key={index} className={tx.isNewCategory ? 'row-suggestion' : 'row-confirmed'}>
                                            <td>{new Date(tx.date).toLocaleDateString('pt-BR')}</td>
                                            <td>{tx.description}</td>
                                            <td className={tx.amount >= 0 ? 'text-success' : 'text-danger'}>
                                                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(tx.amount)}
                                            </td>
                                            <td>
                                                <select 
                                                    value={tx.isNewCategory ? "SUGGESTION" : (tx.categoryId || "")}
                                                    onChange={(e) => handleCategoryChange(index, e.target.value, originalAiTx.suggestedCategoryName)}
                                                    style={{ width: '100%', padding: '5px' }}
                                                >
                                                    <option value="" disabled>-- Selecione --</option>
                                                    
                                                    {originalAiTx.isSuggestion && (
                                                        <option value="SUGGESTION" style={{ fontWeight: 'bold', color: '#d97706' }}>
                                                            ✨ Sugestão: {originalAiTx.suggestedCategoryName}
                                                        </option>
                                                    )}
                                                    
                                                    <optgroup label="As suas Categorias">
                                                        {categories.map(c => (
                                                            <option key={c.id} value={c.id}>{c.name}</option>
                                                        ))}
                                                    </optgroup>
                                                </select>
                                            </td>
                                            <td style={{ textAlign: 'center' }}>
                                                {tx.isNewCategory ? '💡 Nova' : (tx.categoryId ? '✅ Ok' : '⚠️ Pendente')}
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                </div>

                <div className="modal-footer" style={{ display: 'flex', justifyContent: 'space-between', marginTop: '20px' }}>
                    <button onClick={onClose} className="btn-secondary">Cancelar</button>
                    <button 
                        onClick={() => onConfirm(editableTransactions)} 
                        className="btn-primary"
                        disabled={!canSubmit}
                        style={{ opacity: canSubmit ? 1 : 0.5 }}
                    >
                        Confirmar e Salvar {editableTransactions.length} Transações
                    </button>
                </div>
            </div>
        </div>
    );
};