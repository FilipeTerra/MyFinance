// src/components/Transactions/UploadTransactionFileButton.tsx
import './UploadTransactionFileButton.css';

interface UploadTransactionFileButtonProps {
    onClick: () => void;
}

export function UploadTransactionFileButton({ onClick }: UploadTransactionFileButtonProps) {
    return (
        <button 
            className="upload-transaction-file-button" 
            onClick={onClick}
            title="Importar extrato com Inteligência Artificial"
        >
            {/* Ícone SVG moderno (substitui o PNG para melhor resolução e controle de cor) */}
            <svg 
                className="upload-icon" 
                fill="none" 
                stroke="currentColor" 
                viewBox="0 0 24 24" 
                xmlns="http://www.w3.org/2000/svg"
            >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
            </svg>
            
            {/* Se quiser manter o PNG antigo, comente o SVG acima e descomente a linha abaixo */}
            {/* <img className="upload-icon" src="/carregar.png" alt="Upload" /> */}
            
            <span>Importar Extrato</span>
            
            {/* Um charme extra para indicar que tem IA envolvida */}
            <span className="ai-badge">✨ IA</span>
        </button>
    );
}