import type { AccountResponseDto } from '../../types/AccountResponseDto';
import './AccountCard.css';

interface AccountCardProps {
    account: AccountResponseDto;
    onEdit: (account: AccountResponseDto) => void;
    onDelete: (id: string) => void;
}

export function AccountCard({ account, onEdit, onDelete }: AccountCardProps) {
    
    // Formatador de moeda (R$)
    const formatCurrency = (value: number) => {
        return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
    };

    return (
        <div className="account-card">
            <div className="account-card-header">
                <span className="account-type">{account.typeName}</span>
                <div className="account-actions">
                    {/* Botão Editar */}
                    <button 
                        onClick={() => onEdit(account)} 
                        className="btn-icon btn-edit" 
                        title="Editar Conta"
                    >
                        ✏️
                    </button>
                    
                    {/* Botão Excluir */}
                    <button 
                        onClick={() => onDelete(account.id)} 
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