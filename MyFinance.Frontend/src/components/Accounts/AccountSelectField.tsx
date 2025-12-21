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
    
    // Estado interno apenas para o form de criação
    const [newAccountData, setNewAccountData] = useState<NewAccountState>({
        name: '',
        type: '',
        initialBalance: ''
    });

    const { createAccount, isLoading, error, setError } = useTransactionFormLogic();

    // Máscara de Moeda (igual a que você já tinha)
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
        // Se o usuário digitar, limpamos o erro
        if (error) setError(null);
    };

    const handleSaveClick = async () => {
        // 1. Validação Manual Simples
        if (!newAccountData.name.trim()) {
            setError('O nome da conta é obrigatório.');
            return;
        }
        if (newAccountData.type === '') {
            setError('Selecione o tipo da conta.');
            return;
        }
        
        // Conversão do saldo
        let balanceAsNumber = 0;
        if (newAccountData.initialBalance) {
            balanceAsNumber = parseFloat(newAccountData.initialBalance.replace(/\./g, '').replace(',', '.'));
        }

        try {
            // 2. Chamada à API via Hook
            const newAccount = await createAccount({
                name: newAccountData.name,
                type: Number(newAccountData.type) as AccountType,
                initialBalance: balanceAsNumber
            });

            // 3. Sucesso
            onAccountCreated(newAccount); // Atualiza lista global
            onChange(newAccount.id);      // Seleciona a nova conta
            handleCancel();               // Reseta e fecha o modo criação

        } catch (err) {
            console.error(err);
            // O erro já está no state 'error' do hook
        }
    };

    const handleCancel = () => {
        setIsCreating(false);
        setNewAccountData({ name: '', type: '', initialBalance: '' });
        setError(null);
    };

    // --- RENDERIZAÇÃO ---

    // Modo Criação (Formulário Embutido)
    if (isCreating) {
        return (
            <div className="create-item-container">
                <h4>Nova Conta</h4>
                
                {/* Nome */}
                <div className="form-group">
                    <input
                        type="text"
                        placeholder="Nome da Conta (ex: Nubank)"
                        value={newAccountData.name}
                        onChange={(e) => setNewAccountData(prev => ({...prev, name: e.target.value}))}
                        disabled={isLoading}
                        className={error ? 'input-error' : ''}
                    />
                </div>

                {/* Tipo e Saldo lado a lado */}
                <div className="form-row-inline">
                    <div className="form-group" style={{ flex: 1 }}>
                        <select
                            value={newAccountData.type}
                            onChange={(e) => setNewAccountData(prev => ({...prev, type: Number(e.target.value) as AccountType}))}
                            disabled={isLoading}
                        >
                            <option value="" disabled>Tipo</option>
                            {/* Assumindo que AccountType é um objeto/enum. Ajuste conforme sua definição real de tipos */}
                            <option value={AccountType.Carteira}>Carteira</option>
                            <option value={AccountType.ContaCorrente}>Conta Corrente</option>
                            <option value={AccountType.Poupanca}>Poupança</option>
                            <option value={AccountType.CartaoCredito}>Cartão de Crédito</option>
                            <option value={AccountType.Investimento}>Investimento</option>
                            <option value={AccountType.Outro}>Outro</option>
                        </select>
                    </div>

                    <div className="form-group" style={{ flex: 1 }}>
                        <input
                            type="text"
                            placeholder="R$ 0,00"
                            value={newAccountData.initialBalance}
                            onChange={handleAmountChange}
                            inputMode="decimal"
                            disabled={isLoading}
                        />
                    </div>
                </div>

                {error && <div className="field-error-message" style={{marginBottom: '10px'}}>{error}</div>}

                <div className="action-buttons-inline">
                    <button type="button" onClick={handleCancel} className="cancel-button-small" disabled={isLoading}>
                        Cancelar
                    </button>
                    <button type="button" onClick={handleSaveClick} className="save-button-small" disabled={isLoading}>
                        {isLoading ? '...' : 'Salvar Conta'}
                    </button>
                </div>
            </div>
        );
    }

    // Modo Seleção (Select Normal)
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