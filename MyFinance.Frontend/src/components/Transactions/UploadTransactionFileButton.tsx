// src/components/Transactions/UploadTransactionFileButton.tsx
import './UploadTransactionFileButton.css';

interface UploadTransactionFileButtonProps {
    onClick: () => void;
}

export function UploadTransactionFileButton({ onClick }: UploadTransactionFileButtonProps) {
    return (
        <button className="upload-transaction-file-button" onClick={onClick}>
            <img
                className="upload-transaction-file-icon"
                src="/carregar.png"
                alt="Upload"
            />
            Upload Arquivo
        </button>
    );
}
