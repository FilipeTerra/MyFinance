import React, { useState, useEffect } from 'react';
import { api } from '../../services/Api'; 
import type { AiTransactionResponseDto, SaveBatchTransactionRequestDto } from '../../types/AiIntegration';
import type { CategoryResponseDto } from '../../types/CategoryResponseDto';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { ReviewImportModal } from './ReviewImportModal';

interface UploadTransactionModalProps {
    isOpen: boolean;
    onClose: () => void;
    // Restauramos as propriedades antigas para não quebrar a HomePage
    onUpload: (file: File, selectedAccountId: string) => Promise<void>;
    accounts: AccountResponseDto[];
    onAccountCreated?: (newAccount: AccountResponseDto) => void;
}

export const UploadTransactionModal: React.FC<UploadTransactionModalProps> = ({ 
    isOpen, 
    onClose, 
    accounts,
    onUpload // Vamos adaptar esta função original para o novo fluxo
}) => {
    const [file, setFile] = useState<File | null>(null);
    const [accountId, setAccountId] = useState<string>('');
    const [isLoading, setIsLoading] = useState(false);
    const [categories, setCategories] = useState<CategoryResponseDto[]>([]);
    
    const [aiTransactions, setAiTransactions] = useState<AiTransactionResponseDto[]>([]);
    const [isReviewing, setIsReviewing] = useState(false);

    useEffect(() => {
        if (isOpen) {
            // Adicionado o tipo genérico (res: any) para resolver o erro 7006 temporariamente
            api.get('/Category').then((res: any) => setCategories(res.data)).catch(console.error);
        }
    }, [isOpen]);

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files.length > 0) {
            setFile(e.target.files[0]);
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!file || !accountId) return;

        setIsLoading(true);
        try {
            // Em vez de usar diretamente o onUpload antigo, fazemos a chamada direta para a rota de IA
            // Ajuste o endpoint se for diferente no seu Api.ts original
            const formData = new FormData();
            formData.append('file', file);
            formData.append('accountId', accountId);

            const response = await api.post('/Transaction/upload', formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });
            
            setAiTransactions(response.data);
            setIsReviewing(true); 
        } catch (error) {
            alert('Erro ao processar o ficheiro com a IA.');
            console.error(error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleConfirmBatch = async (finalTransactions: SaveBatchTransactionRequestDto[]) => {
        setIsLoading(true);
        try {
            await api.post('/Transaction/batch', finalTransactions);
            alert('Transações guardadas com sucesso!');
            
            setIsReviewing(false);
            setAiTransactions([]);
            setFile(null);
            onClose();
            // Um pequeno truque: recarregar a página para atualizar os dados, 
            // ou pode passar uma função onUploadSuccess na HomePage no futuro.
            window.location.reload(); 
        } catch (error) {
            alert('Erro ao gravar o lote de transações.');
            console.error(error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleCloseModal = () => {
        setIsReviewing(false);
        setAiTransactions([]);
        setFile(null);
        onClose();
    };

    if (!isOpen) return null;

    // Removemos os comentários conflitantes do JSX para resolver os erros 2304, 1005, etc.
    return (
        <>
            {isReviewing ? (
                <ReviewImportModal
                    isOpen={isReviewing}
                    onClose={handleCloseModal}
                    aiTransactions={aiTransactions}
                    categories={categories}
                    onConfirm={handleConfirmBatch}
                />
            ) : (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h2>Importar Extrato (com IA)</h2>
                        <form onSubmit={handleSubmit}>
                            <div className="form-group">
                                <label>Conta de Destino</label>
                                <select required value={accountId} onChange={e => setAccountId(e.target.value)}>
                                    <option value="">Selecione uma conta...</option>
                                    {accounts.map(acc => (
                                        <option key={acc.id} value={acc.id}>{acc.name}</option>
                                    ))}
                                </select>
                            </div>

                            <div className="form-group">
                                <label>Ficheiro CSV</label>
                                <input type="file" accept=".csv" required onChange={handleFileChange} />
                            </div>

                            <div className="modal-actions" style={{ marginTop: '20px', display: 'flex', gap: '10px' }}>
                                <button type="button" onClick={handleCloseModal} className="btn-secondary" disabled={isLoading}>
                                    Cancelar
                                </button>
                                <button type="submit" className="btn-primary" disabled={isLoading || !file || !accountId}>
                                    {isLoading ? 'A IA está a analisar...' : 'Analisar Ficheiro'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </>
    );
};