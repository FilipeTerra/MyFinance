import './TransactionModal.css';
import React, { useEffect } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import type { CategoryDto } from '../Categories/CategorySelectField';
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
    isOpen: boolean;
    onClose: () => void;
    onAccountCreated: (newAccount: AccountResponseDto) => void;
}

const transactionSchema = z.object({
    description: z.string().min(1, 'A Descrição é obrigatória.'),
    amount: z.string().min(1, 'O Valor é obrigatório.')
        .refine((val) => {
            const num = parseFloat(val.replace(/\./g, '').replace(',', '.'));
            return !isNaN(num) && num > 0;
        }, 'O Valor deve ser maior que zero.'),
    type: z.preprocess((val) => Number(val), z.nativeEnum(TransactionType)),
    date: z.string().min(1, 'A Data é obrigatória.'),
    accountId: z.string().min(1, 'A Conta é obrigatória.'),
    categoryId: z.string().min(1, 'A Categoria é obrigatória.'),
});

// Inferir o tipo do formulário a partir do schema
type TransactionFormData = z.infer<typeof transactionSchema>;

export function TransactionModal({ 
    accounts, 
    categories,
    isOpen, 
    onClose, 
    onAccountCreated,
    onCategoryCreated
}: TransactionModalProps) {
    const { 
        createTransaction, 
        isLoading,
        error: apiError,
        setError 
    } = useTransactionFormLogic();

    // Configuração do React Hook Form
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

    useEffect(() => {
        register('accountId');
        register('categoryId');
    }, [register]);

    useEffect(() => {
    if (isOpen) {
        reset({
            description: '',
            amount: '',
            type: TransactionType.Expense,
            date: new Date().toISOString().split('T')[0],
            accountId: '',
            categoryId: ''
        });
        setError(null);
    }}, [isOpen, reset, setError]);

    if (!isOpen) {
        return null;
    }

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
            // CHAMADA REAL
            await createTransaction(transactionDto);
            
            // Sucesso
            onClose(); 
            // Dica: Aqui seria ideal chamar uma prop onTransactionCreated() para atualizar a lista na tela de trás
            
        } catch (err) {
            console.error("Erro ao salvar transação:", err);
            // O hook já preenche 'apiError' com a mensagem do backend
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content">
                <h2>Nova Transação</h2>
                {apiError && <div className="modal-error-message">{apiError}</div>}

                {/* Formulário principal da transação */}
                <form onSubmit={handleSubmit(onSubmit, (errors) => console.log("Erros de validação:", errors))}>
                    <div className="form-group">
                        <label>Tipo da Transação</label>
                          <div className="type-selector">
                            <label>
                                <input 
                                    type="radio" 
                                    value={TransactionType.Expense} 
                                    {...register('type', { valueAsNumber: true })} // REGISTRA NO RHF
                                    disabled={isLoading} 
                                /> Despesa
                            </label>
                            <label>
                                <input 
                                    type="radio" 
                                    value={TransactionType.Income} 
                                    {...register('type', { valueAsNumber: true })} // REGISTRA NO RHF
                                    disabled={isLoading} 
                                /> Receita
                            </label>
                        </div>
                    </div>

                    {/* Descrição */}
                    <div className="form-group">
                        <label htmlFor="description">Descrição</label>
                        <input
                            type="text"
                            id="description"
                            {...register('description')} // Conecta ao RHF
                            placeholder="Ex: Almoço"
                            disabled={isLoading}
                            className={errors.description ? 'input-error' : ''}
                        />
                        {errors.description && <span className="field-error-message">{errors.description.message}</span>}
                    </div>

                    {/* Valor */}
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

                    {/* Data */}
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

                    {/* Conta */}
                    <AccountSelectField
                        accounts={accounts}
                        selectedId={watch('accountId')}
                        onChange={(id) => setValue('accountId', id, { shouldValidate: true })}
                        onAccountCreated={onAccountCreated}
                        errorMessage={errors.accountId?.message}
                        disabled={isLoading}
                    />

                    {/* Categoria */}
                    <CategorySelectField
                        categories={categories}
                        selectedId={watch('categoryId')} // O valor vem do Form
                        onChange={(id) => setValue('categoryId', id, { shouldValidate: true })} // Atualiza o Form
                        errorMessage={errors.categoryId?.message}
                        disabled={isLoading}
                        onCategoryCreated={onCategoryCreated}
                    />

                    {/* Ações do Modal Principal */}
                    <div className="modal-actions">
                        <button type="button" onClick={onClose} className="cancel-button" disabled={isLoading}>Cancelar Transação</button>
                        <button type="submit" className="save-button" disabled={isLoading}>{isLoading ? 'Salvando...' : 'Salvar Transação'}</button>
                    </div>
                </form>
            </div>
        </div>
    );
}