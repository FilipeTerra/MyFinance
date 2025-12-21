import React, { useState, useEffect } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { TransactionType } from '../../types/TransactionType';
import { AccountType } from '../../types/AccountType';
import './TransactionModal.css';
import { AccountField } from '../Accounts/AccountForm';
import { useForm } from 'react-hook-form'; // Controller ajuda com inputs complexos
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTransactionFormLogic } from '../../hooks/useTransactionFormLogic';

interface TransactionModalProps {
    accounts: AccountResponseDto[];
    isOpen: boolean;
    onClose: () => void;
    // onTransactionCreated: (newTransaction: TransactionResponseDto) => void;
    // <<< ADICIONADO: Função para atualizar a lista de contas na HomePage
    onAccountCreated: (newAccount: AccountResponseDto) => void;
}

interface NewAccountState {
    name: string;
    type: AccountType | ''; // Começa vazio
    initialBalance: string;
}

const transactionSchema = z.object({
    description: z.string().min(1, 'A Descrição é obrigatória.'),
    amount: z.string().min(1, 'O Valor é obrigatório.')
        .refine((val) => {
            const num = parseFloat(val.replace(/\./g, '').replace(',', '.'));
            return !isNaN(num) && num > 0;
        }, 'O Valor deve ser maior que zero.'),
    type: z.nativeEnum(TransactionType),
    date: z.string().min(1, 'A Data é obrigatória.'), // Pode adicionar validação de data futura/passada se quiser
    accountId: z.string().min(1, 'A Conta é obrigatória.'),
    categoryId: z.string().min(1, 'A Categoria é obrigatória.'),
});

interface ManualFormErrors {
    [key: string]: string | undefined;
}

// Inferir o tipo do formulário a partir do schema
type TransactionFormData = z.infer<typeof transactionSchema>;

export function TransactionModal({ accounts, isOpen, onClose, onAccountCreated }: TransactionModalProps) {
    const [isCreatingCategory, setIsCreatingCategory] = useState(false);
    const [isCreatingAccount, setIsCreatingAccount] = useState(false);
    const [newCategory, setNewCategory] = useState('');
    const [newAccountData, setNewAccountData] = useState<NewAccountState>({name: '', type: '',initialBalance: ''});
    const [manualErrors, setManualErrors] = useState<ManualFormErrors>({});
    const { 
        createTransaction, 
        createAccount, 
        createCategory, 
        isLoading, // Um único loading para tudo (mais simples)
        error: apiError, // Erros vindos da API
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
    } = useForm<TransactionFormData>({
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

    const currentAccountId = watch('accountId');
    const currentAmount = watch('amount'); 

    useEffect(() => {
        if (isOpen) {
            reset({ // Reseta o form para os valores iniciais
                description: '',
                amount: '',
                type: TransactionType.Expense,
                date: new Date().toISOString().split('T')[0],
                accountId: '',
                categoryId: ''
            });
            setIsCreatingAccount(false);
            setNewAccountData({ name: '', type: '', initialBalance: '' });
            setError(null);
        }
    }, [isOpen, reset]);

    if (!isOpen) {
        return null;
    }

    const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>, field: 'transaction' | 'newAccount') => {
        if (field === 'transaction') {
        } else {
            setManualErrors(prev => ({ ...prev, newAccountBalance: undefined, newAccountGeneral: undefined }));
        }

        const value = e.target.value;
        let digits = value.replace(/\D/g, '');
        
        if (digits === '') {
            if (field === 'transaction') {
                setValue('amount', '', { shouldValidate: true }); // USA O RHF
            } else {
                setNewAccountData(prev => ({ ...prev, initialBalance: '' }));
            }
            return;
        }

        if (digits.length > 1) digits = digits.replace(/^0+/, '');
        while (digits.length < 3) digits = '0' + digits;

        const decimalIndex = digits.length - 2;
        const integerPart = digits.slice(0, decimalIndex);
        const decimalPart = digits.slice(decimalIndex);
        const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
        const formattedValue = formattedInteger + ',' + decimalPart;

        if (field === 'transaction') {
            setValue('amount', formattedValue, { shouldValidate: true }); // USA O RHF
        } else {
            setNewAccountData(prev => ({ ...prev, initialBalance: formattedValue }));
        }
    };


    // <<< Handler para salvar a nova conta
    const handleSaveNewAccount = async () => {
        setManualErrors({}); // Limpa erros gerais
        const validationErrors: ManualFormErrors = {};

        if (!newAccountData.name.trim()) {
            validationErrors.newAccountName = 'O Nome da conta é obrigatório.';
        }
        if (newAccountData.type === '') {
            validationErrors.newAccountType = 'O Tipo da conta é obrigatório.';
        }
        // Permite saldo inicial zero ou negativo, mas valida o formato se preenchido
        const balanceValue = newAccountData.initialBalance.trim();
        let balanceAsNumber = 0; // Default para zero se vazio
        if (balanceValue !== '') {
            balanceAsNumber = parseFloat(balanceValue.replace(/\./g, '').replace(',', '.'));
            if (isNaN(balanceAsNumber)) {
                validationErrors.newAccountBalance = 'Saldo inicial inválido.';
            }
        }

        if (Object.keys(validationErrors).length > 0) {
            setManualErrors(validationErrors);
            return;
        }

        const accountDto = {
            name: newAccountData.name,
            type: Number(newAccountData.type), // Envia como número para API
            initialBalance: balanceAsNumber,
        };

        console.log("Simulando salvar nova conta:", accountDto);
        setManualErrors(prev => ({ ...prev, newAccountGeneral: undefined })); // Limpa erro geral da conta

        try {
            // CHAMADA REAL VIA HOOK
            const newAccount = await createAccount(accountDto);
            
            // Sucesso
            onAccountCreated(newAccount);
            setValue('accountId', newAccount.id, { shouldValidate: true });
            setIsCreatingAccount(false);
            setNewAccountData({ name: '', type: '', initialBalance: '' });

        } catch (err) {
            // O erro genérico fica no 'apiError' do hook, mas se quiser algo específico:
            console.error(err);
            setManualErrors({ newAccountGeneral: "Não foi possível criar a conta." });
        }
    };

    // --- Funções para criação rápida de Categoria (baseado em Conta) ---
    const handleOpenNewCategoryModal = () => {
        setIsCreatingCategory(true);
    };

    // const handleCancelCreateCategory = () => {
    //     setIsCreatingCategory(false);
    //     setNewCategory('');
    //     setErrors(prev => ({ ...prev, categoryId: undefined })); // Limpa erro
    // };

    const handleSaveNewCategory = async () => {
        if (!newCategory.trim()) {
            setManualErrors(prev => ({ ...prev, categoryId: 'O nome da categoria é obrigatório.' }));
            return;
        }
        try {
            const createdCategory = await createCategory({ name: newCategory });
            
            // TODO: Aqui você precisaria atualizar a lista de categorias (se tivesse um state para isso)
            // Por enquanto, apenas selecionamos visualmente ou limpamos
            console.log("Categoria criada:", createdCategory);
            
            setIsCreatingCategory(false);
            setNewCategory('');
            
            // Se você quiser setar o ID no formulário (assumindo que o select suporta esse ID):
            setValue('categoryId', createdCategory.id, { shouldValidate: true });

        } catch (err) {
             console.error(err);
        }
    };
    // --- Fim das Funções de Categoria ---

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
                <form onSubmit={handleSubmit(onSubmit)}> {/* Tipo da Transação */}
                    <div className="form-group">
                        <label>Tipo da Transação</label>
                          <div className="type-selector">
                            <label>
                                <input 
                                    type="radio" 
                                    value={TransactionType.Expense} 
                                    {...register('type')} // REGISTRA NO RHF
                                    disabled={isLoading} 
                                /> Despesa
                            </label>
                            <label>
                                <input 
                                    type="radio" 
                                    value={TransactionType.Income} 
                                    {...register('type')} // REGISTRA NO RHF
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
                            onChange={(e) => handleAmountChange(e, 'transaction')}
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

                    {/* Seção da Conta: Dropdown OU Formulário de Criação */}
                    <AccountField
                        accounts={accounts}
                        accountId={currentAccountId} // Vem do watch('accountId')
                        parentIsLoading={isLoading}
                        parentErrors={{ accountId: errors.accountId?.message }} // Adapta a prop de erro
                        
                        onAccountIdChange={(newId) => {
                            setValue('accountId', newId, { shouldValidate: true });
                        }}

                        onAccountCreated={handleSaveNewAccount}
                    />{/* Fim da account-section */}


                    {/* Categoria */}
                    <div className="form-group form-group-with-button">
                        {!isCreatingCategory ? (
                            <>
                                <div className="input-wrapper">
                                    <label htmlFor="category">Categoria</label>
                                    <select
                                        id="category"
                                        {...register('categoryId')} // RHF Gerencia o value e onChange
                                        disabled={isLoading}
                                        className={errors.categoryId ? 'input-error' : ''}
                                    >
                                        <option value="" disabled>Selecione uma categoria</option>
                                        <option value="temp1">Alimentação (Exemplo)</option>
                                        <option value="temp2">Transporte (Exemplo)</option>
                                    </select>
                                    {/* .message AQUI */}
                                    {errors.categoryId && <span className="field-error-message">{errors.categoryId.message}</span>}
                                </div>
                                <button
                                    type="button" className="add-new-button" aria-label="Criar nova categoria"
                                    onClick={handleOpenNewCategoryModal} // (NOVO)
                                    disabled={isLoading}
                                >+</button>
                            </>
                        ) : (
                            // Modo de criação rápida de Categoria
                            <div className="new-category-group" style={{ width: '100%' }}>
                                <div style={{ flexGrow: 1 }}>
                                    <input
                                        type="text"
                                        placeholder="Nome da nova categoria"
                                        value={newCategory}
                                        onChange={(e) => {
                                            setNewCategory(e.target.value);
                                            setManualErrors(prev => ({ ...prev, categoryId: undefined }));
                                        }}
                                        className={manualErrors.categoryId ? 'input-error' : ''}
                                        disabled={isLoading}
                                        style={{ width: '100%' }}
                                    />
                                    {manualErrors.categoryId && <span className="field-error-message">{manualErrors.categoryId}</span>}
                                </div>
                                <div className="create-category-actions">
                                    <button type="button" onClick={() => setIsCreatingCategory(false)} disabled={isCreatingAccount || isLoading} className="cancel-button">Cancelar</button>
                                    <button type="button" onClick={handleSaveNewCategory} disabled={isCreatingAccount || isLoading} className="save-button">
                                        {isCreatingAccount ? 'Salvando...' : 'Salvar'}
                                    </button>
                                </div>
                            </div>
                        )}
                    </div>
                    {/* Fim do Bloco de Categoria */}


                    {/* Ações do Modal Principal (Salvar Transação / Cancelar) */}
                    {/*  Esconder se estiver criando conta >>> */}
                    {!isCreatingAccount && !isCreatingCategory && (
                        <div className="modal-actions">
                            <button type="button" onClick={onClose} className="cancel-button" disabled={isLoading}>Cancelar Transação</button>
                            <button type="submit" className="save-button" disabled={isLoading}>{isLoading ? 'Salvando...' : 'Salvar Transação'}</button>
                        </div>
                    )}
                </form>
            </div>
        </div>
    );
}