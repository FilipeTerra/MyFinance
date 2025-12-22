import React, { useState } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { AccountType } from '../../types/AccountType';
import { useTransactionFormLogic } from '../../hooks/useTransactionFormLogic';

interface AccountSelectFieldProps {
    accounts: AccountResponseDto[];
    selectedId: string;
    onChange: (id: string) => void;
    onAccountCreated: (newAccount: AccountResponseDto) => void;
    errorMessage?: string;
    disabled?: boolean;
}

interface NewAccountState {
    name: string;
    type: AccountType | '';
    initialBalance: string;
}

export function AccountSelectField({
    accounts,
    selectedId,
    onChange,
    onAccountCreated,
    errorMessage,
    disabled
}: AccountSelectFieldProps) {
    const [isCreating, setIsCreating] = useState(false);
    
    const [newAccountData, setNewAccountData] = useState<NewAccountState>({
        name: '',
        type: '',
        initialBalance: ''
    });

    const { createAccount, isLoading, error, setError } = useTransactionFormLogic();

    const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value;
        let digits = value.replace(/\D/g, '');
        
        if (digits === '') {
            setNewAccountData(prev => ({ ...prev, initialBalance: '' }));
            return;
        }

        if (digits.length > 1) digits = digits.replace(/^0+/, '');
        while (digits.length < 3) digits = '0' + digits;

        const decimalIndex = digits.length - 2;
        const formattedValue = digits.slice(0, decimalIndex) + ',' + digits.slice(decimalIndex);
        
        setNewAccountData(prev => ({ ...prev, initialBalance: formattedValue }));
        if (error) setError(null);
    };

    const handleSaveClick = async () => {
        if (!newAccountData.name.trim()) {
            setError('O nome da conta é obrigatório.');
            return;
        }
        if (newAccountData.type === '') {
            setError('Selecione o tipo da conta.');
            return;
        }
        
        let balanceAsNumber = 0;
        if (newAccountData.initialBalance) {
            balanceAsNumber = parseFloat(newAccountData.initialBalance.replace(/\./g, '').replace(',', '.'));
        }

        try {
            const newAccount = await createAccount({
                name: newAccountData.name,
                type: Number(newAccountData.type) as AccountType,
                initialBalance: balanceAsNumber
            });

            onAccountCreated(newAccount);
            onChange(newAccount.id);
            handleCancel(); 

        } catch (err) {
            console.error(err);
        }
    };

    const handleCancel = () => {
        setIsCreating(false);
        setNewAccountData({ name: '', type: '', initialBalance: '' });
        setError(null);
    };

    // --- MODO CRIAÇÃO: Bloco Padronizado ---
    if (isCreating) {
        return (
            <div className="create-account-form">
                <h4>Nova Conta</h4>
                
                {/* Nome da Conta */}
                <div className="form-group">
                    <label htmlFor="newAccountName">Nome</label>
                    <input
                        id="newAccountName"
                        type="text"
                        placeholder="Ex: Nubank, Carteira..."
                        value={newAccountData.name}
                        onChange={(e) => {
                            setNewAccountData(prev => ({...prev, name: e.target.value}));
                            if(error) setError(null);
                        }}
                        disabled={isLoading}
                        className={error ? 'input-error' : ''}
                        autoFocus
                    />
                </div>

                {/* Linha com Tipo e Saldo lado a lado */}
                <div className="form-row">
                    <div className="form-group">
                        <label htmlFor="newAccountType">Tipo</label>
                        <select
                            id="newAccountType"
                            value={newAccountData.type}
                            onChange={(e) => setNewAccountData(prev => ({...prev, type: Number(e.target.value) as AccountType}))}
                            disabled={isLoading}
                        >
                            <option value="" disabled>Selecione...</option>
                            <option value={AccountType.Carteira}>Carteira</option>
                            <option value={AccountType.ContaCorrente}>Conta Corrente</option>
                            <option value={AccountType.Poupanca}>Poupança</option>
                            <option value={AccountType.CartaoCredito}>Cartão de Crédito</option>
                            <option value={AccountType.Investimento}>Investimento</option>
                            <option value={AccountType.Outro}>Outro</option>
                        </select>
                    </div>

                    <div className="form-group">
                        <label htmlFor="newAccountBalance">Saldo Inicial</label>
                        <input
                            id="newAccountBalance"
                            type="text"
                            placeholder="0,00"
                            value={newAccountData.initialBalance}
                            onChange={handleAmountChange}
                            inputMode="decimal"
                            disabled={isLoading}
                        />
                    </div>
                </div>

                {error && <span className="field-error-message">{error}</span>}

                <div className="create-account-actions">
                    <button 
                        type="button" 
                        onClick={handleCancel} 
                        className="cancel-button" 
                        disabled={isLoading}
                    >
                        Cancelar
                    </button>
                    <button 
                        type="button" 
                        onClick={handleSaveClick} 
                        className="save-button" 
                        disabled={isLoading}
                    >
                        {isLoading ? 'Salvando...' : 'Salvar Conta'}
                    </button>
                </div>
            </div>
        );
    }

    // --- MODO SELEÇÃO (Padrão) ---
    return (
        <div className="form-group form-group-with-button">
            <div className="input-wrapper">
                <label htmlFor="accountId">Conta</label>
                <select
                    id="accountId"
                    value={selectedId}
                    onChange={(e) => onChange(e.target.value)}
                    disabled={disabled}
                    className={errorMessage ? 'input-error' : ''}
                >
                    <option value="" disabled>Selecione uma conta</option>
                    {accounts.map((acc) => (
                        <option key={acc.id} value={acc.id}>
                            {acc.name}
                        </option>
                    ))}
                    {accounts.length === 0 && <option value="" disabled>Nenhuma conta disponível</option>}
                </select>
                {errorMessage && <span className="field-error-message">{errorMessage}</span>}
            </div>
            <button
                type="button"
                className="add-new-button"
                onClick={() => setIsCreating(true)}
                disabled={disabled}
                title="Criar nova conta"
            >
                +
            </button>
        </div>
    );
}