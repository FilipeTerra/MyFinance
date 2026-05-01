import React, { useState, useRef, useEffect } from 'react';
import { api, transactionService } from '../../services/Api'; // Adicionado para fazer as chamadas da IA
import { AccountSelectField } from '../Accounts/AccountSelectField';
import { ReviewImportModal } from './ReviewImportModal'; // O nosso novo modal
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import type { CategoryResponseDto } from '../../types/CategoryResponseDto';
import type { AiTransactionResponseDto, SaveBatchTransactionRequestDto } from '../../types/AiIntegration';
import './TransactionModal.css';

interface UploadTransactionModalProps {
    isOpen: boolean;
    onClose: () => void;
    onUpload: (file: File, accountId: string) => Promise<void>;
    accounts: AccountResponseDto[];
    onAccountCreated: (newAccount: AccountResponseDto) => void;
}

export function UploadTransactionModal({ 
    isOpen, 
    onClose,
    accounts, 
    onAccountCreated 
}: UploadTransactionModalProps) {
    // --- ESTADOS ORIGINAIS ---
    const [accountId, setAccountId] = useState<string>('');
    const [file, setFile] = useState<File | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);

    // --- NOVOS ESTADOS PARA A IA ---
    const [isReviewing, setIsReviewing] = useState(false);
    const [aiTransactions, setAiTransactions] = useState<AiTransactionResponseDto[]>([]);
    const [categories, setCategories] = useState<CategoryResponseDto[]>([]);

    // Busca as categorias ao abrir o modal para passar para a tela de revisão
    useEffect(() => {
        if (isOpen) {
            api.get('/Category').then((res: any) => setCategories(res.data)).catch(console.error);
        }
    }, [isOpen]);

    if (!isOpen) return null;

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files.length > 0) {
            setFile(e.target.files[0]);
        }
    };

    // Modificado para enviar o arquivo para a rota da IA
    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!accountId || !file) {
            alert("Selecione uma conta e um arquivo!");
            return;
        }

        try {
            setIsLoading(true);
            
            const response = await transactionService.uploadFile(file, accountId);
            
            setAiTransactions(response.data);
            setIsReviewing(true);            
        } catch (error) {
            console.error("Erro na IA:", error);
            alert("Erro ao processar o arquivo com a IA.");
        } finally {
            setIsLoading(false);
        }
    };

    // Função que será chamada quando o usuário aprovar a tabela de revisão
    const handleConfirmBatch = async (finalTransactions: SaveBatchTransactionRequestDto[]) => {
        setIsLoading(true);
        try {
            await transactionService.saveBatchTransactions(finalTransactions);
            alert('Transações guardadas com sucesso!');
            
            // Reseta tudo e fecha
            setIsReviewing(false);
            setAiTransactions([]);
            setFile(null);
            setAccountId('');
            onClose();
            
            // Atualiza a tela principal para mostrar os novos dados
            window.location.reload(); 
        } catch (error) {
            alert('Erro ao gravar o lote de transações.');
            console.error(error);
        } finally {
            setIsLoading(false);
        }
    };

    // --- RENDERIZAÇÃO CONDICIONAL ---
    
    // Se a IA já respondeu, renderizamos o modal de revisão
    if (isReviewing) {
        return (
            <ReviewImportModal
                isOpen={isReviewing}
                onClose={() => {
                    setIsReviewing(false);
                    setAiTransactions([]);
                    setFile(null);
                    onClose();
                }}
                aiTransactions={aiTransactions}
                categories={categories}
                onConfirm={handleConfirmBatch}
            />
        );
    }

    // Se ainda não enviou, renderizamos o seu formulário exato e original
    return (
        <div className="modal-overlay">
            <div className="modal-content">
                <h2>Importar Extrato (PDF/CSV)</h2>
                
                <form onSubmit={handleSubmit}>
                    <div className="form-group">
                        <AccountSelectField 
                            accounts={accounts}
                            selectedId={accountId} 
                            onChange={(id) => setAccountId(id)} 
                            onAccountCreated={(newAccount) => {
                                onAccountCreated(newAccount);
                                setAccountId(newAccount.id);
                            }}
                        />
                    </div>

                    <div className="form-group">
                        <label>Arquivo</label>
                        <input 
                            type="file" 
                            ref={fileInputRef}
                            accept=".csv, application/pdf, .pdf"
                            onChange={handleFileChange}
                            required
                        />
                        {file && <span style={{fontSize: '0.8em', color: 'green'}}>Arquivo selecionado: {file.name}</span>}
                    </div>

                    <div className="modal-actions">
                        <button type="button" className="cancel-button" onClick={onClose} disabled={isLoading}>
                            Cancelar
                        </button>
                        <button type="submit" className="save-button" disabled={isLoading || !accountId || !file}>
                            {isLoading ? 'Analisando com IA...' : 'Enviar Arquivo'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}