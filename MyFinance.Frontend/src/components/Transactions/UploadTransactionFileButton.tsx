import './UploadTransactionFileButton.css';

interface UploadTransactionFileButtonProps {
    onClick: () => void; // Mudou de onFileSelect para um simples onClick
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