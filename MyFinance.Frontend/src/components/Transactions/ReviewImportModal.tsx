// src/components/Transactions/ReviewImportModal.tsx
import React, { useState, useEffect } from 'react';
import type { AiTransactionResponseDto, SaveBatchTransactionRequestDto } from '../../types/AiIntegration';
import type { CategoryResponseDto } from '../../types/CategoryResponseDto';
import './ReviewImportModal.css';

// 1. CORREÇÃO: Removemos os 'any' e voltamos a usar as interfaces corretas
interface ReviewImportModalProps {
    isOpen: boolean;
    onClose: () => void;
    aiTransactions: AiTransactionResponseDto[]; 
    categories: CategoryResponseDto[];
    onConfirm: (finalTransactions: SaveBatchTransactionRequestDto[]) => void;
}

interface EditableTransaction extends SaveBatchTransactionRequestDto {
    isCustomEditing: boolean;
    customCategoryText: string;
}

export const ReviewImportModal: React.FC<ReviewImportModalProps> = ({
    isOpen,
    onClose,
    aiTransactions,
    categories,
    onConfirm
}) => {
    const [editableTransactions, setEditableTransactions] = useState<EditableTransaction[]>([]);

    useEffect(() => {
        if (aiTransactions && aiTransactions.length > 0) {
            const initialData: EditableTransaction[] = aiTransactions.map(tx => {
                // Como agora a tipagem é estrita e a rota está certa, podemos confiar nas propriedades em minúsculo
                return {
                    date: tx.date,
                    description: tx.description,
                    amount: tx.amount,
                    accountId: tx.accountId,
                    categoryId: tx.categoryId,
                    newCategoryName: tx.isSuggestion ? tx.suggestedCategoryName : null,
                    isNewCategory: tx.isSuggestion,
                    isCustomEditing: false, 
                    customCategoryText: tx.isSuggestion && tx.suggestedCategoryName ? tx.suggestedCategoryName : ''
                };
            });
            setEditableTransactions(initialData);
        }
    }, [aiTransactions]);

    const handleCategoryChange = (index: number, selectedValue: string, originalSuggestion: string | null) => {
        const updatedList = [...editableTransactions];
        
        if (selectedValue === "SUGGESTION") {
            updatedList[index].isNewCategory = true;
            updatedList[index].categoryId = null;
            updatedList[index].newCategoryName = originalSuggestion;
            updatedList[index].isCustomEditing = false;
            updatedList[index].customCategoryText = originalSuggestion || '';
        } 
        else if (selectedValue === "CUSTOM") {
            updatedList[index].isNewCategory = true;
            updatedList[index].categoryId = null;
            updatedList[index].isCustomEditing = true;
            updatedList[index].customCategoryText = originalSuggestion || ''; 
            updatedList[index].newCategoryName = updatedList[index].customCategoryText;
        }
        else {
            updatedList[index].isNewCategory = false;
            updatedList[index].categoryId = selectedValue;
            updatedList[index].newCategoryName = null;
            updatedList[index].isCustomEditing = false;
        }
        
        setEditableTransactions(updatedList);
    };

    const handleCustomTextChange = (index: number, newText: string) => {
        const updatedList = [...editableTransactions];
        updatedList[index].customCategoryText = newText;
        updatedList[index].newCategoryName = newText;
        setEditableTransactions(updatedList);
    };

    const canSubmit = editableTransactions.every(tx => {
        if (!tx.isNewCategory) return !!tx.categoryId; 
        return tx.newCategoryName && tx.newCategoryName.trim() !== ''; 
    });

    const handleConfirm = () => {
    const payload: SaveBatchTransactionRequestDto[] = editableTransactions.map(tx => {
        const dateAsUtc = new Date(tx.date).toISOString();

        return {
            date: dateAsUtc, 
            description: tx.description,
            amount: tx.amount,
            accountId: tx.accountId,
            categoryId: !tx.isNewCategory ? tx.categoryId : null,
            newCategoryName: tx.isNewCategory ? tx.newCategoryName : null,
            isNewCategory: tx.isNewCategory
        };
    });

    onConfirm(payload);
};

    if (!isOpen) return null;

    return (
        <div className="modal-overlay">
            <div className="modal-content review-modal-content">
                <div className="modal-header">
                    <h2>✨ Revisão Inteligente</h2>
                    <button className="close-button" onClick={onClose}>&times;</button>
                </div>
                
                <div className="modal-body">
                    <p className="modal-subtitle">
                        Revise as classificações sugeridas pela IA. Você pode aceitar, escolher uma categoria existente ou digitar um novo nome.
                    </p>
                    
                    <div className="table-container">
                        <table className="transactions-table">
                            <thead>
                                <tr>
                                    <th>Data</th>
                                    <th>Descrição</th>
                                    <th>Valor</th>
                                    <th className="category-column">Categoria</th>
                                    <th className="status-column">Status</th>
                                </tr>
                            </thead>
                            <tbody>
                                {editableTransactions.map((tx, index) => {
                                    const originalAiTx = aiTransactions[index]; 

                                    return (
                                        <tr key={index} className={tx.isNewCategory ? 'row-suggestion' : 'row-confirmed'}>
                                            <td className="date-cell">{new Date(tx.date).toLocaleDateString('pt-BR')}</td>
                                            <td className="desc-cell" title={tx.description}>{tx.description}</td>
                                            <td className={tx.amount >= 0 ? 'amount-cell text-success' : 'amount-cell text-danger'}>
                                                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(tx.amount)}
                                            </td>
                                            <td className="category-cell">
                                                <select 
                                                    value={tx.isCustomEditing ? "CUSTOM" : (tx.isNewCategory && !tx.isCustomEditing ? "SUGGESTION" : (tx.categoryId || ""))}
                                                    onChange={(e) => handleCategoryChange(index, e.target.value, originalAiTx.suggestedCategoryName || null)}
                                                    className="category-select"
                                                >
                                                    <option value="" disabled>-- Selecione --</option>
                                                    
                                                    {originalAiTx.isSuggestion && (
                                                        <option value="SUGGESTION" className="opt-suggestion">
                                                            ✨ Sugestão: {originalAiTx.suggestedCategoryName}
                                                        </option>
                                                    )}
                                                    
                                                    <option value="CUSTOM" className="opt-custom">
                                                        ➕ Criar Nova Categoria...
                                                    </option>
                                                    
                                                    <optgroup label="Suas Categorias">
                                                        {/* Agora podemos confiar que 'categories' é um array de CategoryResponseDto */}
                                                        {categories && categories.length > 0 ? (
                                                            categories.map(c => (
                                                                <option key={c.id} value={c.id}>
                                                                    {c.name}
                                                                </option>
                                                            ))
                                                        ) : (
                                                            <option disabled>Nenhuma categoria encontrada</option>
                                                        )}
                                                    </optgroup>
                                                </select>

                                                {tx.isCustomEditing && (
                                                    <input 
                                                        type="text"
                                                        className="category-input custom-fade-in"
                                                        value={tx.customCategoryText}
                                                        onChange={(e) => handleCustomTextChange(index, e.target.value)}
                                                        placeholder="Digite o nome da nova categoria..."
                                                        autoFocus
                                                    />
                                                )}
                                            </td>
                                            <td className="status-cell">
                                                {tx.isNewCategory ? (
                                                    <span className="badge badge-new">💡 Nova</span>
                                                ) : (
                                                    tx.categoryId ? <span className="badge badge-ok">✅ Ok</span> : <span className="badge badge-pending">⚠️ Pendente</span>
                                                )}
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                </div>

                <div className="modal-footer">
                    <button onClick={onClose} className="btn-secondary">Cancelar</button>
                    <button 
                        onClick={handleConfirm} 
                        className="btn-primary"
                        disabled={!canSubmit}
                    >
                        Confirmar e Salvar ({editableTransactions.length})
                    </button>
                </div>
            </div>
        </div>
    );
};