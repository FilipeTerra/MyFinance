import React, { useState, useEffect } from 'react';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { TransactionType } from '../../types/TransactionType';
import { AccountType } from '../../types/AccountType'; // <<< ADICIONADO: Importar AccountType
import './TransactionModal.css';
import { AccountField } from '../Accounts/AccountForm';

interface TransactionModalProps {
    accounts: AccountResponseDto[];
    isOpen: boolean;
    onClose: () => void;
    // onTransactionCreated: (newTransaction: TransactionResponseDto) => void;
    // <<< ADICIONADO: Função para atualizar a lista de contas na HomePage
    onAccountCreated: (newAccount: AccountResponseDto) => void;
}

interface FormErrors {
    description?: string;
    amount?: string;
    accountId?: string;
    categoryId?: string;
    general?: string;
    newAccountName?: string;
    newAccountType?: string;
    newAccountBalance?: string;
    newAccountGeneral?: string;
}

// <<< Interface para o estado da nova conta
interface NewAccountState {
    name: string;
    type: AccountType | ''; // Começa vazio
    initialBalance: string;
}

export function TransactionModal({ accounts, isOpen, onClose, onAccountCreated }: TransactionModalProps) { // <<< ALTERADO: Recebe onAccountCreated
    // Estados da Transação (existentes)
    const [description, setDescription] = useState('');
    const [amount, setAmount] = useState('');
    const [type, setType] = useState<TransactionType>(TransactionType.Expense);
    const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
    const [accountId, setAccountId] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [errors, setErrors] = useState<FormErrors>({});
    const [isCreatingCategory, setIsCreatingCategory] = useState(false);
    const [newCategory, setNewCategory] = useState('');
    // <<< Estados para o modo de criação de conta
    const [isCreatingAccount, setIsCreatingAccount] = useState(false);
    const [newAccountData, setNewAccountData] = useState<NewAccountState>({
        name: '',
        type: '',
        initialBalance: ''
    });
    const [isSavingAccount, setIsSavingAccount] = useState(false); // Loading específico para salvar conta

    // Limpa o formulário ao fechar/abrir o modal
    useEffect(() => {
        if (isOpen) {
            setDescription('');
            setAmount('');
            setType(TransactionType.Expense);
            setDate(new Date().toISOString().split('T')[0]);
            setAccountId('');
            setCategoryId('');
            setIsCreatingAccount(false); // Reseta modo de criação de conta
            setNewAccountData({ name: '', type: '', initialBalance: '' }); // Limpa dados da nova conta
            setErrors({});
            setIsLoading(false);
            setIsSavingAccount(false);
        }
    }, [isOpen]);


    if (!isOpen) {
        return null;
    }

    const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>, field: 'transaction' | 'newAccount') => {
        // Limpa erros específicos ao digitar
        if (field === 'transaction') {
            setErrors(prev => ({ ...prev, amount: undefined, general: undefined }));
        } else {
            setErrors(prev => ({ ...prev, newAccountBalance: undefined, newAccountGeneral: undefined }));
        }

        const value = e.target.value;
        let digits = value.replace(/\D/g, ''); // Remove tudo que não for dígito
        if (digits === '') {
            field === 'transaction' ? setAmount('') : setNewAccountData(prev => ({ ...prev, initialBalance: '' }));
            return;
        }

        // Remove zeros à esquerda (exceto se for o único dígito)
        if (digits.length > 1) {
            digits = digits.replace(/^0+/, '');
        }

        // Adiciona zeros à esquerda se necessário para ter pelo menos 3 dígitos (para R$ 0,XX)
        while (digits.length < 3) {
            digits = '0' + digits;
        }

        const decimalIndex = digits.length - 2;
        const integerPart = digits.slice(0, decimalIndex);
        const decimalPart = digits.slice(decimalIndex);

        // Adiciona separador de milhar
        const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');

        const formattedValue = formattedInteger + ',' + decimalPart;

        if (field === 'transaction') {
            setAmount(formattedValue);
        } else {
            setNewAccountData(prev => ({ ...prev, initialBalance: formattedValue }));
        }
    };


    // <<< Handler para salvar a nova conta
    const handleSaveNewAccount = async () => {
        setErrors({}); // Limpa erros gerais
        const validationErrors: FormErrors = {};

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
            setErrors(validationErrors);
            return;
        }

        const accountDto = {
            name: newAccountData.name,
            type: Number(newAccountData.type), // Envia como número para API
            initialBalance: balanceAsNumber,
        };

        console.log("Simulando salvar nova conta:", accountDto);
        setIsSavingAccount(true);
        setErrors(prev => ({ ...prev, newAccountGeneral: undefined })); // Limpa erro geral da conta

        try {
            // ----- SIMULAÇÃO DA API -----
            await new Promise(resolve => setTimeout(resolve, 1000));
            const fakeNewAccount: AccountResponseDto = {
                id: `new-${Date.now()}`, // ID temporário
                name: accountDto.name,
                type: accountDto.type,
                typeName: AccountType[accountDto.type],
                initialBalance: accountDto.initialBalance,
                currentBalance: accountDto.initialBalance, // Inicialmente igual
                userId: 'fake-user-id'
            };
            console.log("Conta criada (simulado):", fakeNewAccount);
            // ----- FIM SIMULAÇÃO -----

            // Chama a função passada por props para atualizar a lista na HomePage
            onAccountCreated(fakeNewAccount);

            // Seleciona a nova conta no dropdown de transação
            setAccountId(fakeNewAccount.id);

            // Volta para o modo de seleção de conta
            setIsCreatingAccount(false);
            setNewAccountData({ name: '', type: '', initialBalance: '' }); // Limpa o form da conta


        } catch (err) {
            // Exemplo de como setar um erro geral vindo da API da conta
            console.error("Erro simulado ao criar conta:", err);
            setErrors({ newAccountGeneral: "Erro ao criar conta (simulado)." });
        } finally {
            setIsSavingAccount(false);
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
            setErrors(prev => ({ ...prev, categoryId: 'O nome da categoria é obrigatório.' }));
            return;
        }
        setIsLoading(true);
        console.log("Simulando criação de categoria:", newCategory);
        // TODO: Chamar categoryService.createCategory(...)
        // Após sucesso:
        // 1. Atualizar a lista de 'categories' (precisa ser buscada)
        // 2. setCategoryId(novaCategoria.id)
        // 3. handleCancelCreateCategory()
        await new Promise(resolve => setTimeout(resolve, 500)); // Simulação
        setIsLoading(false);
        setIsCreatingCategory(false); // Fechar modo de criação
        setNewCategory('');
    };
    // --- Fim das Funções de Categoria ---

    const handleSubmitTransaction = async (e: React.FormEvent) => {
        e.preventDefault();
        setErrors({}); // Limpa todos os erros antes de validar

        const validationErrors: FormErrors = {};

        if (!description.trim()) validationErrors.description = 'A Descrição é obrigatória.';
        if (!accountId) validationErrors.accountId = 'A Conta é obrigatória.'; // Validar se accountId existe agora é crucial
        if (!categoryId) validationErrors.categoryId = 'A Categoria é obrigatória.';

        // Validar valor da transação
        const amountValue = amount.trim();
        let amountAsNumber = NaN;
        if (amountValue === '') {
            validationErrors.amount = 'O Valor é obrigatório.';
        } else {
            amountAsNumber = parseFloat(amountValue.replace(/\./g, '').replace(',', '.'));
            if (isNaN(amountAsNumber)) {
                validationErrors.amount = 'Valor inválido.';
            } else if (amountAsNumber <= 0) {
                validationErrors.amount = 'O Valor deve ser maior que zero.';
            }
        }


        if (Object.keys(validationErrors).length > 0) {
            setErrors(validationErrors);
            return;
        }

        const transactionDto = {
            description,
            amount: amountAsNumber,
            type,
            date: new Date(date).toISOString(), // Enviar como ISO string pode ser mais robusto
            accountId,
            categoryId,
        };

        console.log("Enviando Transação para a API:", transactionDto);
        setIsLoading(true);

        try {
            await new Promise(resolve => setTimeout(resolve, 1000));
            console.log("Transação criada com sucesso (simulado).");
            onClose(); // Fecha o modal
            // TODO: Chamar onTransactionCreated se existir para atualizar lista de transações

        } catch (err) {
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
                {errors.general && <div className="modal-error-message">{errors.general}</div>}

                {/* Formulário principal da transação */}
                <form onSubmit={handleSubmitTransaction}>
                    {/* Tipo da Transação */}
                    <div className="form-group">
                        <label>Tipo da Transação</label>
                        <div className="type-selector">
                            <label>
                                <input type="radio" name="transactionType" value={TransactionType.Expense} checked={type === TransactionType.Expense} onChange={() => setType(TransactionType.Expense)} disabled={isLoading || isSavingAccount} /> Despesa
                            </label>
                            <label>
                                <input type="radio" name="transactionType" value={TransactionType.Income} checked={type === TransactionType.Income} onChange={() => setType(TransactionType.Income)} disabled={isLoading || isSavingAccount} /> Receita
                            </label>
                        </div>
                    </div>

                    {/* Descrição */}
                    <div className="form-group">
                        <label htmlFor="description">Descrição</label>
                        <input
                            type="text" id="description" value={description}
                            onChange={(e) => {
                                setDescription(e.target.value);
                                setErrors(prev => ({ ...prev, description: undefined, general: undefined }));
                            }}
                            placeholder="Ex: Almoço" disabled={isLoading || isSavingAccount}
                            className={errors.description ? 'input-error' : ''}
                        />
                        {errors.description && <span className="field-error-message">{errors.description}</span>}
                    </div>

                    {/* Valor */}
                    <div className="form-group">
                        <label htmlFor="amount">Valor (R$)</label>
                        <input
                            type="text" id="amount" value={amount}
                            onChange={(e) => handleAmountChange(e, 'transaction')}
                            placeholder="0,00" inputMode="decimal" disabled={isLoading || isSavingAccount}
                            className={errors.amount ? 'input-error' : ''}
                        />
                        {errors.amount && <span className="field-error-message">{errors.amount}</span>}
                    </div>

                    {/* Data */}
                    <div className="form-group">
                        <label htmlFor="date">Data</label>
                        <input
                            type="date" id="date" value={date} onChange={(e) => setDate(e.target.value)} disabled={isLoading || isSavingAccount}
                        />
                    </div>

                    {/* Seção da Conta: Dropdown OU Formulário de Criação */}
                    <AccountField
                        accounts={accounts}
                        accountId={accountId}
                        parentIsLoading={isLoading}
                        parentErrors={{ accountId: errors.accountId }}
                        
                        // Callback para atualizar o ID da conta no Modal
                        onAccountIdChange={(newId) => {
                            setAccountId(newId);
                            // Limpa o erro de "conta obrigatória" ao selecionar uma
                            setErrors(prev => ({ ...prev, accountId: undefined, general: undefined }));
                        }}

                        // Callback para quando uma conta for criada
                        onAccountCreated={handleSaveNewAccount}
                    /> {/* Fim da account-section */}


                    {/* Categoria */}
                    <div className="form-group form-group-with-button">
                        {!isCreatingCategory ? (
                            <>
                                <div className="input-wrapper">
                                    <label htmlFor="category">Categoria</label>
                                    <select
                                        id="category" value={categoryId}
                                        onChange={(e) => {
                                            setCategoryId(e.target.value);
                                            setErrors(prev => ({ ...prev, categoryId: undefined, general: undefined }));
                                        }}
                                        disabled={isLoading}
                                        className={errors.categoryId ? 'input-error' : ''}
                                    >
                                        <option value="" disabled>Selecione uma categoria</option>
                                        {/* TODO: Carregar categorias dinamicamente */}
                                        <option value="temp1">Alimentação (Exemplo)</option>
                                        <option value="temp2">Transporte (Exemplo)</option>
                                    </select>
                                    {errors.categoryId && <span className="field-error-message">{errors.categoryId}</span>}
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
                                            setErrors(prev => ({ ...prev, categoryId: undefined }));
                                        }}
                                        className={errors.categoryId ? 'input-error' : ''}
                                        disabled={isLoading}
                                        style={{ width: '100%' }}
                                    />
                                    {errors.categoryId && <span className="field-error-message" style={{ display: 'block' }}>{errors.categoryId}</span>}
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