import './ConfirmationModal.css';

interface ConfirmationModalProps {
    isOpen: boolean;
    onClose: () => void;
    onConfirm: () => void;
    title: string;
    description: string;
    confirmText?: string; // Texto opcional, padrão será 'Excluir'
    cancelText?: string;  // Texto opcional, padrão será 'Cancelar'
    variant?: 'danger' | 'info'; // Para mudar a cor do botão futuramente se precisar
}

export function ConfirmationModal({ 
    isOpen, 
    onClose, 
    onConfirm, 
    title, 
    description, 
    confirmText = 'Excluir',
    cancelText = 'Cancelar'
}: ConfirmationModalProps) {
    
    if (!isOpen) return null;

    return (
        <div className="conf-modal-overlay">
            <div className="conf-modal-content">
                <div className="conf-modal-body">
                    {/* Ícone de Alerta/Lixeira */}
                    <div className="conf-modal-icon-wrapper">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                        </svg>
                    </div>

                    <h3 className="conf-modal-title">{title}</h3>
                    <p className="conf-modal-description">{description}</p>
                </div>

                <div className="conf-modal-actions">
                    <button onClick={onClose} className="conf-modal-btn conf-btn-cancel">
                        {cancelText}
                    </button>
                    <button onClick={onConfirm} className="conf-modal-btn conf-btn-confirm">
                        {confirmText}
                    </button>
                </div>
            </div>
        </div>
    );
}