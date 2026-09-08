// src/components/Transactions/ReviewImportModal.tsx
import React, { useState, useEffect } from 'react';
import type {
    BatchLineError,
    SaveBatchTransactionRequestDto,
    StatementImportResultDto,
} from '../../types/AiIntegration';
import type { CategoryResponseDto } from '../../types/CategoryResponseDto';
import { Modal } from '../Shared/ui/Modal';
import './ReviewImportModal.css';

interface ReviewImportModalProps {
    isOpen: boolean;
    onClose: () => void;
    importResult: StatementImportResultDto;
    categories: CategoryResponseDto[];
    /** Devolve os erros por linha do lote; lista vazia significa que salvou. */
    onConfirm: (finalTransactions: SaveBatchTransactionRequestDto[]) => Promise<BatchLineError[]>;
}

interface EditableTransaction extends SaveBatchTransactionRequestDto {
    isCustomEditing: boolean;
    customCategoryText: string;
    // Só as linhas marcadas vão para o lote. Duplicatas chegam desmarcadas.
    selected: boolean;
    isDuplicate: boolean;
}

export const ReviewImportModal: React.FC<ReviewImportModalProps> = ({
    isOpen,
    onClose,
    importResult,
    categories,
    onConfirm
}) => {
    const [editableTransactions, setEditableTransactions] = useState<EditableTransaction[]>([]);
    // Erro que o servidor apontou, por posição na tabela desta tela.
    const [errosPorLinha, setErrosPorLinha] = useState<Record<number, string>>({});
    const [salvando, setSalvando] = useState(false);
    const aiTransactions = importResult.transactions;

    const selecionadas = editableTransactions.filter(tx => tx.selected);
    // "Pendente" é o que falta categorizar entre o que vai ser salvo — linha
    // desmarcada sem categoria não impede nada.
    const pendentes = selecionadas.filter(tx => !tx.isNewCategory && !tx.categoryId).length;
    const todasSelecionadas = editableTransactions.length > 0
        && selecionadas.length === editableTransactions.length;

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
                    customCategoryText: tx.isSuggestion && tx.suggestedCategoryName ? tx.suggestedCategoryName : '',
                    selected: !tx.isDuplicate,
                    isDuplicate: tx.isDuplicate
                };
            });
            setEditableTransactions(initialData);
        }
    }, [aiTransactions]);

    /** Mexer na linha apaga o erro dela: o aviso antigo já não vale mais. */
    const limparErro = (index: number) => {
        setErrosPorLinha(atual => {
            if (!(index in atual)) return atual;
            const resto = { ...atual };
            delete resto[index];
            return resto;
        });
    };

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
        limparErro(index);
    };

    const handleSelectionChange = (index: number, selected: boolean) => {
        const updatedList = [...editableTransactions];
        updatedList[index].selected = selected;
        setEditableTransactions(updatedList);
        limparErro(index);
    };

    const handleSelectAll = (selected: boolean) => {
        setEditableTransactions(editableTransactions.map(tx => ({ ...tx, selected })));
    };

    const handleCustomTextChange = (index: number, newText: string) => {
        const updatedList = [...editableTransactions];
        updatedList[index].customCategoryText = newText;
        updatedList[index].newCategoryName = newText;
        setEditableTransactions(updatedList);
        limparErro(index);
    };

    const canSubmit = !salvando && selecionadas.length > 0 && selecionadas.every(tx => {
        if (!tx.isNewCategory) return !!tx.categoryId;
        return tx.newCategoryName && tx.newCategoryName.trim() !== '';
    });

    const handleConfirm = async () => {
        // O índice que o servidor devolve é da lista ENVIADA — só as marcadas.
        // Sem guardar esta correspondência, o erro apareceria na linha errada.
        const indicesOriginais: number[] = [];
        const payload: SaveBatchTransactionRequestDto[] = [];

        editableTransactions.forEach((tx, index) => {
            if (!tx.selected) return;

            indicesOriginais.push(index);
            payload.push({
                date: new Date(tx.date).toISOString(),
                description: tx.description,
                amount: tx.amount,
                accountId: tx.accountId,
                categoryId: !tx.isNewCategory ? tx.categoryId : null,
                newCategoryName: tx.isNewCategory ? tx.newCategoryName : null,
                isNewCategory: tx.isNewCategory
            });
        });

        setSalvando(true);
        try {
            const erros = await onConfirm(payload);

            const porLinha: Record<number, string> = {};
            erros.forEach(erro => {
                const indiceOriginal = indicesOriginais[erro.index];
                if (indiceOriginal !== undefined) porLinha[indiceOriginal] = erro.message;
            });
            setErrosPorLinha(porLinha);
        } finally {
            setSalvando(false);
        }
    };

    if (!isOpen) return null;

    return (
        <Modal
            onFechar={onClose}
            titulo={importResult.aiUsed ? '✨ Revisão da importação' : 'Revisão da importação'}
            tamanho="xl"
            corpoRolavel
            className="review-modal-content"
            rodape={
                <>
                    <button onClick={onClose} className="btn-secondary">Cancelar</button>
                    <button
                        onClick={handleConfirm}
                        className="btn-primary"
                        disabled={!canSubmit}
                    >
                        {salvando ? 'Salvando...' : `Confirmar e Salvar (${selecionadas.length})`}
                    </button>
                </>
            }
        >
                    <p className="modal-subtitle">
                        {importResult.parserUsed
                            ? `Arquivo lido por: ${importResult.parserUsed}. `
                            : ''}
                        Revise as categorias antes de salvar. Você pode aceitar a sugestão, escolher uma
                        categoria existente ou digitar um novo nome.
                    </p>

                    {importResult.aiUnavailable && (
                        <p className="modal-aviso" role="status">
                            ⚠️ O agente de IA está indisponível. As transações foram lidas normalmente e
                            classificadas pelo que você já registrou antes
                            {pendentes > 0
                                ? `; ${pendentes} ainda precisa(m) de categoria.`
                                : '.'}
                        </p>
                    )}

                    {!importResult.aiUnavailable && importResult.warnings.map(warning => (
                        <p key={warning} className="modal-aviso" role="status">⚠️ {warning}</p>
                    ))}

                    {importResult.duplicateCount > 0 && (
                        <p className="modal-aviso" role="status">
                            🔁 {importResult.duplicateCount} lançamento(s) já existem nesta conta e vieram
                            desmarcados. Se alguma dessas compras aconteceu de novo, marque a linha para importá-la.
                        </p>
                    )}

                    {/* Fica fora do cabeçalho da tabela de propósito: no celular o
                        `<thead>` vira sr-only, e um "marcar todos" escondido lá
                        dentro sumiria justo onde marcar em lote é mais trabalhoso. */}
                    <div className="review-barra-selecao">
                        <span className="review-contagem">
                            <strong>{selecionadas.length}</strong> de {editableTransactions.length} selecionados
                        </span>
                        <button
                            type="button"
                            className="review-btn-selecionar"
                            onClick={() => handleSelectAll(!todasSelecionadas)}
                        >
                            {todasSelecionadas ? 'Desmarcar todos' : 'Marcar todos'}
                        </button>
                    </div>

                    <div className="table-container">
                        {/* Os `role` explícitos não são redundantes: no celular o CSS
                            troca o `display` de table/tr/td para virar cartões, e isso
                            apaga a semântica implícita de tabela. Sem eles, um leitor
                            de tela perderia a relação entre cada valor e sua coluna. */}
                        <table className="transactions-table" role="table">
                            <thead className="transactions-table-head" role="rowgroup">
                                <tr role="row">
                                    <th role="columnheader" scope="col" className="select-column">Importar</th>
                                    <th role="columnheader" scope="col">Data</th>
                                    <th role="columnheader" scope="col">Descrição</th>
                                    <th role="columnheader" scope="col">Valor</th>
                                    <th role="columnheader" scope="col" className="category-column">Categoria</th>
                                    <th role="columnheader" scope="col" className="status-column">Status</th>
                                </tr>
                            </thead>
                            <tbody role="rowgroup">
                                {editableTransactions.map((tx, index) => {
                                    const originalAiTx = aiTransactions[index];
                                    const erro = errosPorLinha[index];

                                    return (
                                        <tr
                                            key={index}
                                            role="row"
                                            className={[
                                                tx.isNewCategory ? 'row-suggestion' : 'row-confirmed',
                                                tx.isDuplicate ? 'row-duplicate' : '',
                                                tx.selected ? '' : 'row-unselected',
                                                erro ? 'row-error' : ''
                                            ].filter(Boolean).join(' ')}
                                        >
                                            <td role="cell" className="select-cell">
                                                {/* O <label> é o alvo de toque: dar padding ao próprio
                                                    checkbox não aumenta a área clicável dele. */}
                                                <label className="select-toque">
                                                    <input
                                                        type="checkbox"
                                                        checked={tx.selected}
                                                        onChange={(e) => handleSelectionChange(index, e.target.checked)}
                                                        aria-label={`Importar ${tx.description}`}
                                                    />
                                                </label>
                                            </td>
                                            <td role="cell" className="date-cell">{new Date(tx.date).toLocaleDateString('pt-BR')}</td>
                                            <td role="cell" className="desc-cell" title={tx.description}>{tx.description}</td>
                                            <td role="cell" className={tx.amount >= 0 ? 'amount-cell text-success' : 'amount-cell text-danger'}>
                                                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(tx.amount)}
                                            </td>
                                            <td role="cell" className="category-cell">
                                                <select
                                                    value={tx.isCustomEditing ? "CUSTOM" : (tx.isNewCategory && !tx.isCustomEditing ? "SUGGESTION" : (tx.categoryId || ""))}
                                                    onChange={(e) => handleCategoryChange(index, e.target.value, originalAiTx.suggestedCategoryName || null)}
                                                    className="category-select"
                                                    aria-label={`Categoria de ${tx.description}`}
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
                                            <td role="cell" className="status-cell">
                                                {erro ? (
                                                    <span className="review-erro-linha">⛔ {erro}</span>
                                                ) : tx.isDuplicate ? (
                                                    <span className="badge badge-duplicate">🔁 Já importada</span>
                                                ) : tx.isNewCategory ? (
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
        </Modal>
    );
};
