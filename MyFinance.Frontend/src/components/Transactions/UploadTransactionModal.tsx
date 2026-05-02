// src/components/Transactions/UploadTransactionModal.tsx
import React, { useState, useRef, useEffect } from 'react';
import { transactionService, categoryService } from '../../services/Api'; 
import { AccountSelectField } from '../Accounts/AccountSelectField';
import { ReviewImportModal } from './ReviewImportModal';
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
    const [accountId, setAccountId] = useState<string>('');
    const [file, setFile] = useState<File | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);

    const [isReviewing, setIsReviewing] = useState(false);
    const [aiTransactions, setAiTransactions] = useState<AiTransactionResponseDto[]>([]);
    const [categories, setCategories] = useState<CategoryResponseDto[]>([]);

    useEffect(() => {
        if (isOpen) {
            const fetchCategories = async () => {
                try {
                    const response = await categoryService.getAll();
                    setCategories(response.data);
                } catch (error) {
                    console.error("Erro ao buscar categorias para o modal:", error);
                }
            };
            fetchCategories();
        }
    }, [isOpen]);

    if (!isOpen) return null;

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files.length > 0) {
            setFile(e.target.files[0]);
        }
    };

    const handleTriggerFile = () => {
        if (fileInputRef.current) {
            fileInputRef.current.click();
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!accountId || !file) return;

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

    const handleConfirmBatch = async (finalTransactions: SaveBatchTransactionRequestDto[]) => {
        setIsLoading(true);
        try {
            await transactionService.saveBatchTransactions(finalTransactions);
            
            setIsReviewing(false);
            setAiTransactions([]);
            setFile(null);
            setAccountId('');
            onClose();
            window.location.reload(); 
        } catch (error) {
            alert('Erro ao gravar o lote de transações.');
            console.error(error);
        } finally {
            setIsLoading(false);
        }
    };

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

    return (
        <div className="modal-overlay">
            <div className="modal-content" style={{ maxWidth: '450px' }}>
                <div className="modal-header">
                    <h2>Importar Extrato</h2>
                    <button className="close-button" onClick={onClose} disabled={isLoading}>&times;</button>
                </div>
                
                <form onSubmit={handleSubmit} className="modal-body" style={{ display: 'flex', flexDirection: 'column', gap: '20px', paddingTop: '10px' }}>
                    
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
                        {/* 1. CORREÇÃO: O Label agora reflete as duas opções */}
                        <label style={{ fontWeight: 600, color: '#334155' }}>Arquivo (CSV ou PDF)</label>
                        
                        <div 
                            style={{
                                border: '2px dashed #cbd5e1',
                                borderRadius: '8px',
                                padding: '24px',
                                textAlign: 'center',
                                backgroundColor: file ? '#f0fdf4' : '#f8fafc',
                                borderColor: file ? '#86efac' : '#cbd5e1',
                                cursor: 'pointer',
                                transition: 'all 0.2s',
                                display: 'flex',
                                flexDirection: 'column',
                                alignItems: 'center',
                                justifyContent: 'center',
                                gap: '8px'
                            }}
                            onClick={handleTriggerFile}
                        >
                            {/* 2. CORREÇÃO: Restauramos o suporte ao .pdf no input */}
                            <input 
                                type="file" 
                                ref={fileInputRef}
                                accept=".csv, application/pdf, .pdf"
                                onChange={handleFileChange}
                                style={{ display: 'none' }}
                                required
                            />
                            
                            {file ? (
                                <>
                                    <svg width="24" height="24" fill="none" stroke="#16a34a" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                    <span style={{ color: '#15803d', fontWeight: 500, fontSize: '0.9rem', wordBreak: 'break-all' }}>{file.name}</span>
                                    <span style={{ fontSize: '0.75rem', color: '#16a34a' }}>Clique para alterar</span>
                                </>
                            ) : (
                                <>
                                    <svg width="28" height="28" fill="none" stroke="#64748b" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" /></svg>
                                    {/* 3. CORREÇÃO: O texto descritivo avisa o usuário do PDF */}
                                    <span style={{ color: '#475569', fontSize: '0.9rem' }}>Clique aqui para selecionar seu CSV ou PDF</span>
                                </>
                            )}
                        </div>
                    </div>

                    <div className="modal-footer" style={{ borderTop: 'none', padding: '10px 0 0 0', backgroundColor: 'transparent' }}>
                        <button type="button" className="btn-secondary" onClick={onClose} disabled={isLoading}>
                            Cancelar
                        </button>
                        <button 
                            type="submit" 
                            className="btn-primary" 
                            disabled={isLoading || !accountId || !file}
                            style={{ 
                                display: 'flex', 
                                alignItems: 'center', 
                                gap: '6px',
                                opacity: (!accountId || !file) ? 0.5 : 1
                            }}
                        >
                            {isLoading ? (
                                <>
                                    <span className="spinner">⏳</span> Processando IA...
                                </>
                            ) : (
                                <>
                                    Enviar <span className="ai-badge" style={{ backgroundColor: 'rgba(255,255,255,0.2)', color: 'white', border: 'none', marginLeft: '2px' }}>✨</span>
                                </>
                            )}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}