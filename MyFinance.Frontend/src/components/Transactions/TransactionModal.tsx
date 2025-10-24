// src/components/Transactions/TransactionModal.tsx
import React, { useState } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { TransactionType } from '../../types/TransactionType';
import './TransactionModal.css';

interface TransactionModalProps {
    accounts: AccountResponseDto[];
    isOpen: boolean;
    onClose: () => void;
    // onTransactionCreated: (newTransaction: TransactionResponseDto) => void;
}

// Interface para o estado de erros
interface FormErrors {
    description?: string;
    amount?: string;
    accountId?: string;
    categoryId?: string;
    general?: string; // Para erros gerais da API
}

export function TransactionModal({ accounts, isOpen, onClose }: TransactionModalProps) {
    const [description, setDescription] = useState('');
    const [amount, setAmount] = useState('');
    const [type, setType] = useState<TransactionType>(TransactionType.Expense);
    const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
    const [accountId, setAccountId] = useState('');
    const [categoryId, setCategoryId] = useState('');
    // const [newCategory, setNewCategory] = useState(''); // Removido temporariamente
    const [isLoading, setIsLoading] = useState(false);

    // (Mudança 1) Estado de erros agora é um objeto
    const [errors, setErrors] = useState<FormErrors>({});

    if (!isOpen) {
        return null;
    }

    const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        // Limpa o erro específico do amount ao digitar
        setErrors(prev => ({ ...prev, amount: undefined, general: undefined }));
        const value = e.target.value;
        let digits = value.replace(/\D/g, '');
        if (digits === '') {
            setAmount('');
            return;
        }
        digits = digits.replace(/^0+/, '');
        while (digits.length < 3) {
            digits = '0' + digits;
        }
        const decimalIndex = digits.length - 2;
        const formattedValue = digits.slice(0, decimalIndex) + ',' + digits.slice(decimalIndex);
        setAmount(formattedValue);
    };

    const handleOpenNewAccountModal = () => {
        console.log("TODO: Abrir modal de criação rápida de conta.");
    };

    // const handleCreateCategory = () => { // Removido temporariamente
    //     console.log("TODO: Criar nova categoria:", newCategory);
    // };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setErrors({}); // Limpa todos os erros antes de validar

        // (Mudança 2) Cria um objeto para acumular os erros da validação atual
        const validationErrors: FormErrors = {};

        if (!description.trim()) {
            validationErrors.description = 'A Descrição é obrigatória.';
        }
        if (amount === '' || amount === '0,00') {
            validationErrors.amount = 'O Valor deve ser maior que zero.';
        }
        if (!accountId) {
            validationErrors.accountId = 'A Conta é obrigatória.';
        }
        if (!categoryId) {
            validationErrors.categoryId = 'A Categoria é obrigatória.';
        }

        const amountAsNumber = parseFloat(amount.replace(',', '.'));
        if (amount !== '' && (isNaN(amountAsNumber) || amountAsNumber <= 0)) {
            // Adiciona erro ao 'amount' se já não tiver um erro mais específico
            if (!validationErrors.amount) {
                validationErrors.amount = 'Valor inválido.';
            }
        }

        // Se houver qualquer erro de validação, atualiza o estado e para
        if (Object.keys(validationErrors).length > 0) {
            setErrors(validationErrors);
            return;
        }

        // Se passou na validação, continua para preparar e enviar
        const dto = {
            description,
            amount: amountAsNumber, // Já validamos que não é NaN e > 0
            type,
            date: new Date(date),
            accountId,
            categoryId,
        };

        console.log("Enviando para a API:", dto);
        setIsLoading(true);

        try {
            await new Promise(resolve => setTimeout(resolve, 1000));
            console.log("Transação criada com sucesso (simulado).");
            onClose(); // Fecha o modal
            // TODO: Chamar onTransactionCreated se existir

        } catch (err) {
            // Exemplo de como setar um erro geral vindo da API
            setErrors({ general: "Erro ao salvar transação (simulado)." });
            console.error("Erro simulado:", err);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content">
                <h2>Nova Transação</h2>
                {/* Mostra erro geral (da API, por exemplo) */}
                {errors.general && <div className="modal-error-message">{errors.general}</div>}

                <form onSubmit={handleSubmit}>
                    {/* Seletor de Tipo (Sem validação específica aqui) */}
                    <div className="form-group">
                        <label>Tipo da Transação</label>
                        <div className="type-selector">
                            <label>
                                <input type="radio" name="transactionType" value={TransactionType.Expense} checked={type === TransactionType.Expense} onChange={() => setType(TransactionType.Expense)} disabled={isLoading} /> Despesa
                            </label>
                            <label>
                                <input type="radio" name="transactionType" value={TransactionType.Income} checked={type === TransactionType.Income} onChange={() => setType(TransactionType.Income)} disabled={isLoading} /> Receita
                            </label>
                        </div>
                    </div>

                    {/* Descrição */}
                    <div className="form-group">
                        <label htmlFor="description">Descrição</label>
                        <input
                            type="text" id="description" value={description}
                            // (Mudança 3) Limpa erro específico ao digitar + className condicional
                            onChange={(e) => {
                                setDescription(e.target.value);
                                setErrors(prev => ({ ...prev, description: undefined, general: undefined }));
                            }}
                            placeholder="Ex: Almoço" disabled={isLoading}
                            className={errors.description ? 'input-error' : ''} // (Mudança 3)
                        />
                        {/* (Mudança 4) Mostra erro específico */}
                        {errors.description && <span className="field-error-message">{errors.description}</span>}
                    </div>

                    {/* Valor */}
                    <div className="form-group">
                        <label htmlFor="amount">Valor (R$)</label>
                        <input
                            type="text" id="amount" value={amount}
                            onChange={handleAmountChange} // Já limpa o erro 'amount'
                            placeholder="0,00" inputMode="numeric" disabled={isLoading}
                            className={errors.amount ? 'input-error' : ''} // (Mudança 3)
                        />
                        {/* (Mudança 4) Mostra erro específico */}
                        {errors.amount && <span className="field-error-message">{errors.amount}</span>}
                    </div>

                    {/* Data (Sem validação específica aqui) */}
                    <div className="form-group">
                        <label htmlFor="date">Data</label>
                        <input
                            type="date" id="date" value={date} onChange={(e) => setDate(e.target.value)} disabled={isLoading}
                        />
                        {/* Poderia adicionar validação/erro aqui se necessário */}
                    </div>

                    {/* Conta */}
                    <div className="form-group form-group-with-button">
                        <div className="input-wrapper">
                            <label htmlFor="account">Conta</label>
                            <select
                                id="account" value={accountId}
                                // (Mudança 3) Limpa erro específico ao selecionar + className condicional
                                onChange={(e) => {
                                    setAccountId(e.target.value);
                                    setErrors(prev => ({ ...prev, accountId: undefined, general: undefined }));
                                }}
                                disabled={isLoading}
                                className={errors.accountId ? 'input-error' : ''} // (Mudança 3)
                            >
                                <option value="" disabled>Selecione uma conta</option>
                                {accounts.map(account => (
                                    <option key={account.id} value={account.id}>{account.name}</option>
                                ))}
                            </select>
                            {/* (Mudança 4) Mostra erro específico */}
                            {errors.accountId && <span className="field-error-message">{errors.accountId}</span>}
                        </div>
                        <button type="button" className="add-new-button" aria-label="Criar nova conta" onClick={handleOpenNewAccountModal} disabled={isLoading}>+</button>
                    </div>

                    {/* Categoria */}
                    <div className="form-group form-group-with-button">
                        <div className="input-wrapper">
                            <label htmlFor="category">Categoria</label>
                            <select
                                id="category" value={categoryId}
                                // (Mudança 3) Limpa erro específico ao selecionar + className condicional
                                onChange={(e) => {
                                    setCategoryId(e.target.value);
                                    setErrors(prev => ({ ...prev, categoryId: undefined, general: undefined }));
                                }}
                                disabled={isLoading}
                                className={errors.categoryId ? 'input-error' : ''} // (Mudança 3)
                            >
                                <option value="" disabled>Selecione uma categoria</option>
                                {/* TODO: Carregar categorias */}
                                <option value="temp1">Alimentação (Exemplo)</option>
                                <option value="temp2">Transporte (Exemplo)</option>
                            </select>
                            {/* (Mudança 4) Mostra erro específico */}
                            {errors.categoryId && <span className="field-error-message">{errors.categoryId}</span>}
                        </div>
                        <button
                            type="button" className="add-new-button" aria-label="Criar nova categoria"
                            onClick={() => console.log("TODO: Abrir modal de criação rápida de categoria.")}
                            disabled={isLoading}
                        >+</button>
                    </div>

                    {/* Ações */}
                    <div className="modal-actions">
                        <button type="button" onClick={onClose} className="cancel-button" disabled={isLoading}>Cancelar</button>
                        <button type="submit" className="save-button" disabled={isLoading}>{isLoading ? 'Salvando...' : 'Salvar'}</button>
                    </div>
                </form>
            </div>
        </div>
    );
}