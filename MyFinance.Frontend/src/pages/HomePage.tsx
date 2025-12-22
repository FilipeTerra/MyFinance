import './HomePage.css';
import { useEffect, useState } from 'react';
import { Header } from '../components/Layout/Header';
import type { TransactionResponseDto } from '../types/TransactionResponseDto';
import type { AccountResponseDto } from '../types/AccountResponseDto';
import type { CategoryResponseDto } from '../types/CategoryResponseDto';
import { TransactionFilter } from '../components/Transactions/TransactionFilter';
import { TransactionList } from '../components/Transactions/TransactionList';
import { CreateTransactionButton } from '../components/Transactions/CreateTransactionButton';
import { TransactionModal } from '../components/Transactions/TransactionModal';
import { accountService, categoryService, transactionService, AxiosError, type ApiErrorResponse } from '../services/Api';
import { CreateAccountButton } from '../components/Accounts/CreateAccountButton';

// Componentes de Conta
import { AccountCard } from '../components/Accounts/AccountCard';
import { AccountModal } from '../components/Accounts/AccountModal';

interface FiltersState {
    accountId: string;
    searchText?: string;
    date?: string;
    amount?: number;
    page?: number;
    pageSize?: number;
}

export function HomePage() {
    const [accounts, setAccounts] = useState<AccountResponseDto[]>([]);
    
    // Estados para Transações
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [transactions, setTransactions] = useState<TransactionResponseDto[]>([]);
    const [activeFilters, setActiveFilters] = useState<FiltersState | null>(null);
    const [isLoadingAccounts, setIsLoadingAccounts] = useState(true);
    const [isLoadingTransactions, setIsLoadingTransactions] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [categories, setCategories] = useState<CategoryResponseDto[]>([]);
    const [transactionToEdit, setTransactionToEdit] = useState<TransactionResponseDto | null>(null);

    // <<< ESTADOS PARA CONTA >>>
    const [isAccountModalOpen, setIsAccountModalOpen] = useState(false);
    const [accountToEdit, setAccountToEdit] = useState<AccountResponseDto | null>(null);

    // Carregar dados iniciais
    useEffect(() => {
        const loadInitialData = async () => {
            setIsLoadingAccounts(true);
            try {
                const [accountsRes, categoriesRes] = await Promise.all([
                    accountService.getAllAccounts(),
                    categoryService.getAll()
                ]);

                // Ordena contas por nome
                setAccounts(accountsRes.data.sort((a, b) => a.name.localeCompare(b.name)));
                setCategories(categoriesRes.data);
            } catch (error) {
                console.error("Erro ao carregar dados:", error);
                setError("Erro ao carregar dados iniciais.");
            } finally {
                setIsLoadingAccounts(false);
            }
        };

        loadInitialData();
    }, []);

    // Buscar transações quando filtros mudam
    useEffect(() => {
        if (!activeFilters || !activeFilters.accountId) {
            setTransactions([]);
            return;
        }

        const fetchTransactions = async () => {
            try {
                setError(null);
                setIsLoadingTransactions(true);
                const response = await transactionService.getTransactions(activeFilters);
                setTransactions(response.data);
            } catch (err) {
                const axiosError = err as AxiosError<ApiErrorResponse>;
                setError(axiosError.response?.data?.message || 'Erro ao buscar transações.');
            } finally {
                setIsLoadingTransactions(false);
            }
        };

        fetchTransactions();

    }, [activeFilters]);

    const handleFilterChange = (filters: Omit<FiltersState, 'page' | 'pageSize'>) => {
        setActiveFilters({
            ...filters,
            page: 1,
            pageSize: 20
        } as FiltersState);
    };

    const handleCategoryCreated = (newCategory: CategoryResponseDto) => {
        setCategories(prev => [...prev, newCategory]);
    };

    // --- LÓGICA DE TRANSAÇÕES ---
    const handleDeleteTransaction = async (id: string) => {
        try {
            await transactionService.delete(id);
            setTransactions(prevTransactions => 
                prevTransactions.filter(tx => tx.id !== id)
            );
            alert('Transação excluída com sucesso!'); 
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            alert(axiosError.response?.data?.message || "Erro ao tentar excluir a transação.");
        }
    };

    const handleOpenCreateModal = () => {
        setTransactionToEdit(null); 
        setIsModalOpen(true);
    };

    const handleOpenEditModal = (transaction: TransactionResponseDto) => {
        setTransactionToEdit(transaction);
        setIsModalOpen(true);
    };

    const handleCloseModal = () => {
        setIsModalOpen(false);
        setTransactionToEdit(null); 
        if (activeFilters) {
            setActiveFilters({ ...activeFilters });
        }
    };

    // --- LÓGICA DE CONTAS ---

    // Abre modal para criar NOVA conta
    const handleOpenCreateAccountModal = () => {
        setAccountToEdit(null);
        setIsAccountModalOpen(true);
    };

    // Abre modal para EDITAR conta existente
    const handleEditAccount = (account: AccountResponseDto) => {
        setAccountToEdit(account);
        setIsAccountModalOpen(true);
    };

    // Callback executado quando o AccountModal salva com sucesso
    const handleAccountSaved = (savedAccount: AccountResponseDto, isEdit: boolean) => {
        setAccounts(prevAccounts => {
            let updatedList = [];
            if (isEdit) {
                // Substitui a conta antiga pela atualizada
                updatedList = prevAccounts.map(acc => acc.id === savedAccount.id ? savedAccount : acc);
            } else {
                // Adiciona a nova conta
                updatedList = [...prevAccounts, savedAccount];
            }
            // Mantém ordenado
            return updatedList.sort((a, b) => a.name.localeCompare(b.name));
        });
    };

    // Compatibilidade com o TransactionModal (que também cria conta)
    const handleAccountCreatedFromTransaction = (newAccount: AccountResponseDto) => {
        handleAccountSaved(newAccount, false);
    };

    // Exclusão de conta
    const handleDeleteAccount = async (id: string) => {
        if (!confirm('Tem certeza que deseja excluir esta conta? Todas as transações vinculadas a ela serão perdidas permanentemente.')) {
            return;
        }

        try {
            await accountService.delete(id);
            
            // Remove da lista visual
            setAccounts(prev => prev.filter(acc => acc.id !== id));
            
            // Se a conta excluída estava selecionada no filtro, limpa o filtro
            if (activeFilters?.accountId === id) {
                setActiveFilters(prev => prev ? { ...prev, accountId: '' } : null);
                setTransactions([]); // Limpa a lista de transações pois o filtro sumiu
            }
            
            alert('Conta excluída com sucesso!');
        } catch (err) {
            console.error("Erro ao excluir conta:", err);
            const axiosError = err as AxiosError<ApiErrorResponse>;
            alert(axiosError.response?.data?.message || 'Erro ao excluir conta. Verifique se existem transações.');
        }
    };

    return (
        <div className="homepage-container">
            <Header />

            <main className="homepage-content">
                <h2>Contas e Transações</h2>

                {error && <div className="error-message">{error}</div>}

                <div className="homepage-actions">
                    <CreateAccountButton onClick={handleOpenCreateAccountModal} />
                    <CreateTransactionButton onClick={handleOpenCreateModal} />
                </div>

                {/* Seção de Cards de Contas */}
                {isLoadingAccounts ? (
                    <p>Carregando contas...</p>
                ) : (
                    <div className="accounts-grid">
                        {/* Lista as contas existentes */}
                        {accounts.map(account => (
                            <AccountCard 
                                key={account.id} 
                                account={account} 
                                onEdit={handleEditAccount}
                                onDelete={handleDeleteAccount}
                            />
                        ))}
                    </div>
                )}

                <TransactionFilter
                    accounts={accounts}
                    onFilterChange={handleFilterChange}
                    isLoading={isLoadingTransactions}
                />

                <TransactionList
                    transactions={transactions}
                    isLoading={isLoadingTransactions}
                    onDelete={handleDeleteTransaction}
                    onEdit={handleOpenEditModal} 
                />
            </main>

            {/* Modal de Transação */}
            <TransactionModal
                accounts={accounts}
                categories={categories}
                isOpen={isModalOpen}
                onClose={handleCloseModal}
                onAccountCreated={handleAccountCreatedFromTransaction}
                onCategoryCreated={handleCategoryCreated}
                transactionToEdit={transactionToEdit}
            />

            {/* <<< Modal de Conta >>> */}
            <AccountModal 
                isOpen={isAccountModalOpen}
                onClose={() => setIsAccountModalOpen(false)}
                onSuccess={handleAccountSaved}
                accountToEdit={accountToEdit}
            />
        </div>
    );
}