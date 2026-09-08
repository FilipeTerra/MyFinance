// src/components/Transactions/UploadTransactionModal.tsx
import React, { useState, useRef, useEffect } from 'react';
import { transactionService, categoryService, mensagemDeErro } from '../../services/Api';
import { AccountSelectField } from '../Accounts/AccountSelectField';
import { ReviewImportModal } from './ReviewImportModal';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import type { CategoryResponseDto } from '../../types/CategoryResponseDto';
import type {
    BatchLineError,
    SaveBatchResponse,
    SaveBatchTransactionRequestDto,
    StatementImportResultDto,
} from '../../types/AiIntegration';
import { Modal } from '../Shared/ui/Modal';
import { FeedbackModal } from '../Shared/ui/FeedbackModal';
import { useFeedback } from '../../hooks/useFeedback';
import './TransactionModal.css';
import './UploadTransactionModal.css';

const MAX_FILES = 5;

interface UploadTransactionModalProps {
    isOpen: boolean;
    onClose: () => void;
    accounts: AccountResponseDto[];
    onAccountCreated: (newAccount: AccountResponseDto) => void;
    onTransactionSaved: () => void;
}

function formatarTamanho(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function UploadTransactionModal({
    isOpen,
    onClose,
    accounts,
    onAccountCreated,
    onTransactionSaved
}: UploadTransactionModalProps) {
    const [accountId, setAccountId] = useState<string>('');
    const [files, setFiles] = useState<File[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);

    const [isReviewing, setIsReviewing] = useState(false);
    const [importResult, setImportResult] = useState<StatementImportResultDto | null>(null);
    const [categories, setCategories] = useState<CategoryResponseDto[]>([]);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    const { feedback, mostrarSucesso, mostrarErro, fechar: fecharFeedback } = useFeedback();

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
        const selecionados = Array.from(e.target.files ?? []);
        if (selecionados.length === 0) return;

        // Rejeita a seleção inteira em vez de truncar em silêncio: um usuário que
        // escolheu 6 arquivos esperando que todos entrassem não deveria descobrir
        // só depois que 1 ficou de fora sem aviso.
        if (selecionados.length > MAX_FILES) {
            setErrorMessage(`Selecione no máximo ${MAX_FILES} arquivos por vez.`);
            e.target.value = '';
            return;
        }

        setFiles(selecionados);
        setErrorMessage(null);
        e.target.value = '';
    };

    const handleRemoveFile = (index: number) => {
        setFiles(prev => prev.filter((_, i) => i !== index));
    };

    const handleTriggerFile = () => {
        if (fileInputRef.current) {
            fileInputRef.current.click();
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!accountId || files.length === 0) return;

        try {
            setIsLoading(true);
            setErrorMessage(null);

            const response = await transactionService.uploadFiles(files, accountId);
            setImportResult(response.data);
            setIsReviewing(true);
        } catch (error) {
            // A API responde 400 com o mesmo envelope quando não consegue ler
            // nenhum arquivo — a mensagem dela é mais útil que um texto genérico.
            const apiMessage = (error as { response?: { data?: { message?: string } } })
                ?.response?.data?.message;

            console.error('Erro ao importar o(s) extrato(s):', error);
            setErrorMessage(apiMessage ?? 'Não foi possível ler o(s) arquivo(s). Verifique o formato e tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    const encerrarImportacao = () => {
        setIsReviewing(false);
        setImportResult(null);
        setFiles([]);
        setAccountId('');
        onClose();
        onTransactionSaved();
    };

    const handleConfirmBatch = async (
        finalTransactions: SaveBatchTransactionRequestDto[],
    ): Promise<BatchLineError[]> => {
        setIsLoading(true);
        try {
            // A API aprende as associações descrição → categoria dentro da mesma
            // transação do lote, então não há um segundo passo de aprendizado aqui.
            const resultado = await transactionService.saveBatchTransactions(finalTransactions);

            // A revisão só fecha quando o usuário dispensa a confirmação: fechar
            // antes desmontaria o popup junto e ninguém leria nada.
            mostrarSucesso(resultado.message, { aoFechar: encerrarImportacao });
            return [];
        } catch (error) {
            console.error('Erro ao salvar o lote de transações:', error);

            const resposta = (error as { response?: { data?: SaveBatchResponse } })?.response?.data;
            const erros = resposta?.errors ?? [];

            mostrarErro(
                mensagemDeErro(error, 'Não foi possível salvar o lote. Nenhuma transação foi gravada.'),
                {
                    titulo: 'Nada foi salvo',
                    detalhes: erros.map(e => `Linha ${e.index + 1} — ${e.description}: ${e.message}`),
                },
            );

            // A revisão continua aberta com tudo preenchido para o usuário corrigir.
            return erros;
        } finally {
            setIsLoading(false);
        }
    };

    const popupFeedback = feedback && (
        <FeedbackModal
            variante={feedback.variante}
            titulo={feedback.titulo}
            mensagem={feedback.mensagem}
            detalhes={feedback.detalhes}
            onFechar={fecharFeedback}
        />
    );

    if (isReviewing && importResult) {
        return (
            <>
                <ReviewImportModal
                    isOpen={isReviewing}
                    onClose={() => {
                        setIsReviewing(false);
                        setImportResult(null);
                        setFiles([]);
                        onClose();
                    }}
                    importResult={importResult}
                    categories={categories}
                    onConfirm={handleConfirmBatch}
                />
                {popupFeedback}
            </>
        );
    }

    const rotuloEnviar = files.length > 1 ? `Enviar ${files.length} arquivos` : 'Enviar';

    return (
        <>
        <Modal onFechar={onClose} titulo="Importar Extrato" tamanho="md">
                <form onSubmit={handleSubmit} className="upload-form">

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
                        <label style={{ fontWeight: 600, color: '#334155' }}>
                            Arquivos (CSV, TXT ou PDF — até {MAX_FILES})
                        </label>

                        <div
                            style={{
                                border: '2px dashed #cbd5e1',
                                borderRadius: '8px',
                                padding: '24px',
                                textAlign: 'center',
                                backgroundColor: files.length > 0 ? '#f0fdf4' : '#f8fafc',
                                borderColor: files.length > 0 ? '#86efac' : '#cbd5e1',
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
                            <input
                                type="file"
                                ref={fileInputRef}
                                accept=".csv, .txt, application/pdf, .pdf"
                                onChange={handleFileChange}
                                style={{ display: 'none' }}
                                multiple
                            />

                            {files.length > 0 ? (
                                <>
                                    <svg width="24" height="24" fill="none" stroke="#16a34a" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                    <span style={{ color: '#15803d', fontWeight: 500, fontSize: '0.9rem' }}>
                                        {files.length === 1 ? '1 arquivo selecionado' : `${files.length} arquivos selecionados`}
                                    </span>
                                    <span style={{ fontSize: '0.75rem', color: '#16a34a' }}>Clique para escolher outros</span>
                                </>
                            ) : (
                                <>
                                    <svg width="28" height="28" fill="none" stroke="#64748b" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" /></svg>
                                    <span style={{ color: '#475569', fontSize: '0.9rem' }}>Clique aqui para selecionar seus arquivos</span>
                                </>
                            )}
                        </div>

                        {files.length > 0 && (
                            <ul className="upload-lista-arquivos">
                                {files.map((f, index) => (
                                    <li key={`${f.name}-${index}`} className="upload-arquivo-item">
                                        <span className="upload-arquivo-nome" title={f.name}>{f.name}</span>
                                        <span className="upload-arquivo-tamanho">{formatarTamanho(f.size)}</span>
                                        <button
                                            type="button"
                                            className="upload-arquivo-remover"
                                            onClick={() => handleRemoveFile(index)}
                                            aria-label={`Remover ${f.name}`}
                                        >
                                            ×
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </div>

                    {errorMessage && (
                        <div className="upload-erro" role="alert">
                            {errorMessage}
                        </div>
                    )}

                    <div className="upload-acoes">
                        <button type="button" className="btn-secondary" onClick={onClose} disabled={isLoading}>
                            Cancelar
                        </button>
                        <button
                            type="submit"
                            className="btn-primary upload-btn-enviar"
                            disabled={isLoading || !accountId || files.length === 0}
                        >
                            {isLoading ? (
                                <>
                                    <span className="spinner">⏳</span> Processando arquivo(s)...
                                </>
                            ) : (
                                rotuloEnviar
                            )}
                        </button>
                    </div>
                </form>
        </Modal>
        {popupFeedback}
        </>
    );
}
