// src/components/Transactions/AccountField.tsx
import React, { useState } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { AccountType } from '../../types/AccountType';
// import { accountService } from '../../services/Api'; // Necessário para salvar de verdade

// Importe o CSS do modal se os estilos estiverem lá
import '../Transactions/TransactionModal.css';
        
// Props que o componente pai (TransactionModal) irá passar
interface AccountFieldProps {
    accounts: AccountResponseDto[];
    accountId: string; // O ID da conta selecionada (controlado pelo pai)
    onAccountIdChange: (id: string) => void; // Função para avisar o pai que o ID mudou
    onAccountCreated: () => void; // Função para avisar o pai que uma nova conta foi criada (para ele recarregar a lista)
    parentErrors: { accountId?: string }; // Erros de validação do pai (ex: "Conta é obrigatória")
    parentIsLoading: boolean; // Se o modal principal está em loading
}

// Erros internos do formulário de nova conta
interface NewAccountErrors {
    newAccountName?: string;
    newAccountType?: string;
    newAccountBalance?: string;
    newAccountGeneral?: string;
}

const initialNewAccountData = {
    name: '',
    type: AccountType.ContaCorrente, // Um valor padrão
    initialBalance: '' // Armazena o valor formatado como string
};

export function AccountField({
    accounts,
    accountId,
    onAccountIdChange,
    onAccountCreated,
    parentErrors,
    parentIsLoading
}: AccountFieldProps) {

    // Estados internos para o sub-formulário de criação
    const [isCreatingAccount, setIsCreatingAccount] = useState(false);
    const [isSavingAccount, setIsSavingAccount] = useState(false);
    const [newAccountData, setNewAccountData] = useState(initialNewAccountData);
    const [internalErrors, setInternalErrors] = useState<NewAccountErrors>({});

    // Handler local para o campo de valor (saldo inicial)
    const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setInternalErrors(prev => ({ ...prev, newAccountBalance: undefined, newAccountGeneral: undefined }));
        const value = e.target.value;
        let digits = value.replace(/\D/g, '');
        if (digits === '') {
            setNewAccountData(prev => ({ ...prev, initialBalance: '' }));
            return;
        }
        digits = digits.replace(/^0+/, '');
        while (digits.length < 3) {
            digits = '0' + digits;
        }
        const decimalIndex = digits.length - 2;
        const formattedValue = digits.slice(0, decimalIndex) + ',' + digits.slice(decimalIndex);
        setNewAccountData(prev => ({ ...prev, initialBalance: formattedValue }));
    };

    // Handler para salvar a nova conta
    const handleSaveNewAccount = async () => {
        // Validação interna
        const validationErrors: NewAccountErrors = {};
        if (!newAccountData.name.trim()) {
            validationErrors.newAccountName = "O nome é obrigatório.";
        }
        if (newAccountData.initialBalance === '' || newAccountData.initialBalance === '0,00') {
            // Você pode ajustar essa regra se saldo 0 for permitido
            validationErrors.newAccountBalance = "O saldo inicial é obrigatório e deve ser maior que zero.";
        }

        if (Object.keys(validationErrors).length > 0) {
            setInternalErrors(validationErrors);
            return;
        }

        setIsSavingAccount(true);
        setInternalErrors({});

        try {
            // --- Aqui você chamaria a API de verdade ---
            // const balance = parseFloat(newAccountData.initialBalance.replace(',', '.'));
            // const dto = { name: newAccountData.name, type: newAccountData.type, initialBalance: balance };
            // const newAccount = await accountService.createAccount(dto);
            
            // Simulação de sucesso:
            console.log("Simulando salvar conta:", newAccountData);
            await new Promise(resolve => setTimeout(resolve, 700));
            const newSimulatedAccountId = "id-simulado-" + newAccountData.name;
            // Fim da simulação

            // Sucesso!
            onAccountCreated(); // 1. Avisa o pai para recarregar a lista de contas
            onAccountIdChange(newSimulatedAccountId); // 2. Avisa o pai para selecionar a nova conta
            setIsCreatingAccount(false); // 3. Fecha o formulário de criação
            setNewAccountData(initialNewAccountData); // 4. Reseta o form interno

        } catch (err) {
            setInternalErrors({ newAccountGeneral: "Erro ao salvar a conta. (Simulado)" });
        } finally {
            setIsSavingAccount(false);
        }
    };

    // O JSX que você forneceu, agora usando os estados e props deste componente:
    return (
        <div className="account-section"> {/* Wrapper que você passou */}
            {isCreatingAccount ? (
                // <<< Formulário para criar nova conta >>>
                <div className="create-account-form">
                    <h4>Nova Conta</h4>
                    {internalErrors.newAccountGeneral && <div className="modal-error-message">{internalErrors.newAccountGeneral}</div>}

                    <div className="form-group">
                        <label htmlFor="new-account-name">Nome da Conta</label>
                        <input
                            type="text" id="new-account-name"
                            value={newAccountData.name}
                            onChange={(e) => {
                                setNewAccountData(prev => ({ ...prev, name: e.target.value }));
                                setInternalErrors(prev => ({ ...prev, newAccountName: undefined, newAccountGeneral: undefined }));
                            }}
                            placeholder="Ex: Banco ABC"
                            disabled={isSavingAccount || parentIsLoading}
                            className={internalErrors.newAccountName ? 'input-error' : ''}
                        />
                        {internalErrors.newAccountName && <span className="field-error-message">{internalErrors.newAccountName}</span>}
                    </div>
                    <div className="form-group">
                        <label htmlFor="new-account-type">Tipo</label>
                        <select
                            id="new-account-type"
                            value={newAccountData.type}
                            onChange={(e) => {
                                setNewAccountData(prev => ({ ...prev, type: Number(e.target.value) as AccountType }));
                                setInternalErrors(prev => ({ ...prev, newAccountType: undefined, newAccountGeneral: undefined }));
                            }}
                            disabled={isSavingAccount || parentIsLoading}
                            className={internalErrors.newAccountType ? 'input-error' : ''}
                        >
                            {/* Mapeia o Enum AccountType para options */}
                            {Object.entries(AccountType)
                                .filter(([key]) => isNaN(Number(key))) // Filtra chaves numéricas
                                .map(([key, value]) => (
                                    <option key={value} value={value}>{key.replace(/([A-Z])/g, ' $1').trim()}</option> 
                                ))}
                        </select>
                        {internalErrors.newAccountType && <span className="field-error-message">{internalErrors.newAccountType}</span>}
                    </div>
                    <div className="form-group">
                        <label htmlFor="new-account-balance">Saldo Inicial (R$)</label>
                        <input
                            type="text" id="new-account-balance"
                            value={newAccountData.initialBalance}
                            onChange={handleAmountChange} // Usando o handler local
                            placeholder="0,00" inputMode="decimal"
                            disabled={isSavingAccount || parentIsLoading}
                            className={internalErrors.newAccountBalance ? 'input-error' : ''}
                        />
                        {internalErrors.newAccountBalance && <span className="field-error-message">{internalErrors.newAccountBalance}</span>}
                    </div>
                    <div className="create-account-actions">
                        <button type="button" onClick={() => setIsCreatingAccount(false)} disabled={isSavingAccount || parentIsLoading} className="cancel-button">Cancelar</button>
                        <button type="button" onClick={handleSaveNewAccount} disabled={isSavingAccount || parentIsLoading} className="save-button">
                            {isSavingAccount ? 'Salvando...' : 'Salvar Conta'}
                        </button>
                    </div>
                </div>
            ) : (
                // <<< Dropdown de conta existente (como antes) >>>
                <div className="form-group form-group-with-button">
                    <div className="input-wrapper">
                        <label htmlFor="account">Conta</label>
                        <select
                            id="account" value={accountId}
                            onChange={(e) => {
                                onAccountIdChange(e.target.value); // Avisa o pai da mudança
                            }}
                            disabled={parentIsLoading || isSavingAccount} // Desabilita se o pai OU o filho estiverem ocupados
                            className={parentErrors.accountId ? 'input-error' : ''} // Usa o erro do pai
                        >
                            <option value="" disabled>Selecione uma conta</option>
                            {accounts.map(account => (
                                <option key={account.id} value={account.id}>{account.name}</option>
                            ))}
                        </select>
                        {parentErrors.accountId && <span className="field-error-message">{parentErrors.accountId}</span>}
                    </div>
                    <button
                        type="button" className="add-new-button" aria-label="Criar nova conta"
                        onClick={() => setIsCreatingAccount(true)} // Apenas muda o estado interno
                        disabled={parentIsLoading || isSavingAccount}>
                        +
                    </button>
                </div>
            )}
        </div>
    );
}