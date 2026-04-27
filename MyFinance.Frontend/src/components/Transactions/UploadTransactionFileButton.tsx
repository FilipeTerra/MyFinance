import { useRef } from 'react';
import './UploadTransactionFileButton.css';

interface UploadTransactionFileButtonProps {
    // Agora o botão devolve o arquivo selecionado em vez de um clique genérico
    onFileSelect: (file: File) => void;
}

export function UploadTransactionFileButton({ onFileSelect }: UploadTransactionFileButtonProps) {
    // Referência para acessar o input HTML invisível
    const fileInputRef = useRef<HTMLInputElement>(null);

    const handleButtonClick = () => {
        // Dispara o clique no input oculto
        fileInputRef.current?.click();
    };

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (file) {
            onFileSelect(file);
        }
        // Limpa o input para permitir selecionar o mesmo arquivo novamente se der erro
        event.target.value = '';
    };

    return (
        <>
            <input
                type="file"
                ref={fileInputRef}
                style={{ display: 'none' }} // Esconde o input original
                accept=".csv, .txt" // Limita aos formatos que o Agente Python vai ler
                onChange={handleFileChange}
            />
            <button className="upload-transaction-file-button" onClick={handleButtonClick}>
                <img
                    className="upload-transaction-file-icon"
                    src="/carregar.png"
                    alt="Upload"
                />
                Upload Arquivo
            </button>
        </>
    );
}