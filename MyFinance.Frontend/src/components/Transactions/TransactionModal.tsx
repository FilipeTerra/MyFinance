import './TransactionModal.css';
import React, { useEffect } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import type { CategoryDto } from '../Categories/CategorySelectField';
import type { TransactionResponseDto } from '../../types/TransactionResponseDto';
import { TransactionType } from '../../types/TransactionType';
import { CategorySelectField } from '../Categories/CategorySelectField';
import { AccountSelectField } from '../Accounts/AccountSelectField';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTransactionFormLogic } from '../../hooks/useTransactionFormLogic';

interface TransactionModalProps {
    accounts: AccountResponseDto[];
    categories: CategoryDto[]; 
    onCategoryCreated: (newCategory: CategoryDto) => void;
    onAccountCreated: (newAccount: AccountResponseDto) => void;
    isOpen: boolean;
    onClose: () => void;
    transactionToEdit?: TransactionResponseDto | null;
}

// Schema de validação
const transactionSchema = z.object({
    description: z.string().min(1, 'A Descrição é obrigatória.'),
    amount: z.string().min(1, 'O Valor é obrigatório.')
        .refine((val) => {
            // Aceita "1000", "1.000,00" ou "1000.00"
            const num = parseFloat(val.replace(/\./g, '').replace(',', '.'));
            return !isNaN(num) && num > 0;
        }, 'O Valor deve ser maior que zero.'),
    // Preprocess garante que "1" vire 1 (number) antes de validar
    type: z.preprocess((val) => Number(val), z.nativeEnum(TransactionType)),
    date: z.string().min(1, 'A Data é obrigatória.'),
    accountId: z.string().min(1, 'A Conta é obrigatória.'),
    categoryId: z.string().min(1, 'A Categoria é obrigatória.'),
});

type TransactionFormData = z.infer<typeof transactionSchema>;

export function TransactionModal({ 
    accounts, 
    categories,
    isOpen, 
    onClose, 
    onAccountCreated,
    onCategoryCreated,
    transactionToEdit
}: TransactionModalProps) {
    const { 
        createTransaction, 
        updateTransaction,
        isLoading,
        error: apiError,
        setError 
    } = useTransactionFormLogic();

    // CORREÇÃO AQUI: Removemos <TransactionFormData> do useForm.
    // Deixamos o zodResolver inferir os tipos automaticamente.
    const {
        register,
        handleSubmit,
        setValue,
        reset,
        watch,
        formState: { errors }
    } = useForm({
        resolver: zodResolver(transactionSchema),
        defaultValues: {
            description: '',
            amount: '',
            type: TransactionType.Expense,
            date: new Date().toISOString().split('T')[0],
            accountId: '',
            categoryId: ''
        }
    });

    const currentAmount = watch('amount'); 

    // Registra campos que não são inputs nativos (selects customizados)
    useEffect(() => {
        register('accountId');
        register('categoryId');
    }, [register]);

    // Efeito para preencher o formulário (Edição) ou Limpar (Criação)
    useEffect(() => {
        if (isOpen) {
            setError(null);
            
            if (transactionToEdit) {
                // --- MODO EDIÇÃO ---
                const formattedAmount = transactionToEdit.amount
                    .toFixed(2)
                    .replace('.', ',')
                    .replace(/\B(?=(\d{3})+(?!\d))/g, '.');

                const formattedDate = new Date(transactionToEdit.date).toISOString().split('T')[0];

                reset({
                    description: transactionToEdit.description,
                    amount: formattedAmount,
                    type: transactionToEdit.type,
                    date: formattedDate,
                    accountId: transactionToEdit.accountId,
                    categoryId: transactionToEdit.categoryId
                });
            } else {
                // --- MODO CRIAÇÃO ---
                reset({
                    description: '',
                    amount: '',
                    type: TransactionType.Expense,
                    date: new Date().toISOString().split('T')[0],
                    accountId: '',
                    categoryId: ''
                });
            }
        }
    }, [isOpen, transactionToEdit, reset, setError]);

    if (!isOpen) return null;

    // Formata o valor monetário enquanto digita
    const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value;
        let digits = value.replace(/\D/g, '');
        
        if (digits === '') {
            setValue('amount', '', { shouldValidate: true });
            return;
        }

        if (digits.length > 1) digits = digits.replace(/^0+/, '');
        while (digits.length < 3) digits = '0' + digits;

        const decimalIndex = digits.length - 2;
        const integerPart = digits.slice(0, decimalIndex);
        const decimalPart = digits.slice(decimalIndex);
        const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
        const formattedValue = formattedInteger + ',' + decimalPart;

        setValue('amount', formattedValue, { shouldValidate: true });
    };

    const onSubmit = async (data: TransactionFormData) => {
        const amountAsNumber = parseFloat(data.amount.replace(/\./g, '').replace(',', '.'));

        const transactionDto = {
            description: data.description,
            amount: amountAsNumber,
            type: data.type,
            date: new Date(data.date).toISOString(),
            accountId: data.accountId,
            categoryId: data.categoryId,
        };

        try {
            if (transactionToEdit) {
                await updateTransaction(transactionToEdit.id, transactionDto);
            } else {
                await createTransaction(transactionDto);
            }
            
            onClose(); 
        } catch (err) {
            console.error("Erro ao salvar transação:", err);
        }
    };

    const isEditing = !!transactionToEdit;
    const modalTitle = isEditing ? 'Editar Transação' : 'Nova Transação';
    const buttonText = isLoading ? 'Salvando...' : (isEditing ? 'Salvar Alterações' : 'Salvar Transação');

    return (
        <div className="modal-overlay">
            <div className="modal-content">
                <h2>{modalTitle}</h2>
                {apiError && <div className="modal-error-message">{apiError}</div>}

                <form onSubmit={handleSubmit(onSubmit, (errors) => console.log("Erros:", errors))}>
                    
                    <div className="form-group">
                        <label>Tipo da Transação</label>
                          <div className="type-selector">
                            <label>
                                <input 
                                    type="radio" 
                                    value={TransactionType.Expense} 
                                    {...register('type', { valueAsNumber: true })} 
                                    disabled={isLoading} 
                                /> Despesa
                            </label>
                            <label>
                                <input 
                                    type="radio" 
                                    value={TransactionType.Income} 
                                    {...register('type', { valueAsNumber: true })} 
                                    disabled={isLoading} 
                                /> Receita
                            </label>
                        </div>
                    </div>

                    <div className="form-group">
                        <label htmlFor="description">Descrição</label>
                        <input
                            type="text"
                            id="description"
                            {...register('description')}
                            placeholder="Ex: Almoço"
                            disabled={isLoading}
                            className={errors.description ? 'input-error' : ''}
                        />
                        {errors.description && <span className="field-error-message">{errors.description.message}</span>}
                    </div>

                    <div className="form-group">
                        <label htmlFor="amount">Valor (R$)</label>
                        <input
                            type="text" 
                            id="amount" 
                            {...register('amount')} 
                            onChange={handleAmountChange}
                            value={currentAmount || ''} 
                            placeholder="0,00" 
                            inputMode="decimal" 
                            disabled={isLoading}
                            className={errors.amount ? 'input-error' : ''}
                        />
                        {errors.amount && <span className="field-error-message">{errors.amount.message}</span>}
                    </div>

                    <div className="form-group">
                        <label htmlFor="date">Data</label>
                        <input
                            type="date"
                            id="date"
                            {...register('date')}
                            disabled={isLoading}
                        />
                        {errors.date && <span className="field-error-message">{errors.date.message}</span>}
                    </div>

                    <AccountSelectField
                        accounts={accounts}
                        selectedId={watch('accountId')}
                        onChange={(id) => setValue('accountId', id, { shouldValidate: true })}
                        onAccountCreated={onAccountCreated}
                        errorMessage={errors.accountId?.message}
                        disabled={isLoading}
                        allowCreation={!isEditing}
                    />

                    <CategorySelectField
                        categories={categories}
                        selectedId={watch('categoryId')}
                        onChange={(id) => setValue('categoryId', id, { shouldValidate: true })}
                        errorMessage={errors.categoryId?.message}
                        disabled={isLoading}
                        onCategoryCreated={onCategoryCreated}
                        allowCreation={!isEditing}
                    />

                    <div className="modal-actions">
                        <button type="button" onClick={onClose} className="cancel-button" disabled={isLoading}>
                            Cancelar
                        </button>
                        <button type="submit" className="save-button" disabled={isLoading}>
                            {buttonText}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}