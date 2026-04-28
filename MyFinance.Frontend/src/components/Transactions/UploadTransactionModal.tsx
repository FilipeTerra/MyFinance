import { useState, useRef } from 'react';
import { AccountSelectField } from '../Accounts/AccountSelectField';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import './TransactionModal.css';

interface UploadTransactionModalProps {
    isOpen: boolean;
    onClose: () => void;
    onUpload: (file: File, accountId: string) => Promise<void>;
    // 1. Adicionamos estas duas propriedades para o select funcionar
    accounts: AccountResponseDto[];
    onAccountCreated: (newAccount: AccountResponseDto) => void;
}

export function UploadTransactionModal({ 
    isOpen, 
    onClose, 
    onUpload, 
    accounts, 
    onAccountCreated 
}: UploadTransactionModalProps) {
    const [accountId, setAccountId] = useState<string>('');
    const [file, setFile] = useState<File | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);

    if (!isOpen) return null;

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files.length > 0) {
            setFile(e.target.files[0]);
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!accountId || !file) {
            alert("Selecione uma conta e um arquivo!");
            return;
        }

        try {
            setIsLoading(true);
            await onUpload(file, accountId);
            setAccountId('');
            setFile(null);
            onClose();
        } catch (error) {
            console.error(error);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content">
                <h2>Importar Extrato (PDF/CSV)</h2>
                
                <form onSubmit={handleSubmit}>
                    <div className="form-group">
                        {/* 2. Propriedades corrigidas para respeitar a interface */}
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
                            {isLoading ? 'Processando...' : 'Enviar Arquivo'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}