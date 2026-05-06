import type { AccountResponseDto } from '../../types/AccountResponseDto';
import './AccountCard.css';

interface AccountCardProps {
    account: AccountResponseDto;
    onEdit: (account: AccountResponseDto) => void;
    onDelete: (id: string) => void;
    onSelect?: (id: string) => void;
    selected?: boolean;
}

export function AccountCard({ account, onEdit, onDelete, onSelect, selected = false }: AccountCardProps) {
    
    // Formatador de moeda (R$)
    const formatCurrency = (value: number) => {
        return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
    };

    const formatAccountTypeName = (typeName: string) => {
        return typeName
            .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
            .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2')
            .trim();
    };

    return (
        <div className={`account-card ${selected ? 'selected' : ''}`} onClick={() => onSelect?.(account.id)}>
            <div className="account-card-header">
                <span className="account-type">{formatAccountTypeName(account.typeName)}</span>
                <div className="account-actions">
                    {/* Botão Editar */}
                    <button 
                        onClick={(e) => { e.stopPropagation(); onEdit(account); }}
                        className="btn-icon btn-edit" 
                        title="Editar Conta"
                    >
                        ✏️
                    </button>
                    
                    {/* Botão Excluir */}
                    <button 
                        onClick={(e) => { e.stopPropagation(); onDelete(account.id); }}
                        className="btn-icon btn-delete" 
                        title="Excluir Conta"
                    >
                        🗑️
                    </button>
                </div>
            </div>
            
            <h3 className="account-name">{account.name}</h3>
            
            <div className={`account-balance ${account.currentBalance < 0 ? 'negative' : 'positive'}`}>
                {formatCurrency(account.currentBalance)}
            </div>
        </div>
    );
}