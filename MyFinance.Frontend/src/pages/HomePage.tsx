import { useEffect, useState } from 'react';
import { Header } from '../components/Layout/Header';
import type { TransactionResponseDto } from '../types/TransactionResponseDto';
import { TransactionFilter } from '../components/Transactions/TransactionFilter';
import { TransactionList } from '../components/Transactions/TransactionList';
import { CreateTransactionButton } from '../components/Transactions/CreateTransactionButton';
import { TransactionModal } from '../components/Transactions/TransactionModal';
import './HomePage.css';
import { accountService, categoryService, transactionService, AxiosError, type ApiErrorResponse } from '../services/Api';
import type { AccountResponseDto } from '../types/AccountResponseDto';
import type { CategoryResponseDto } from '../types/CategoryResponseDto';
import { AccountCard } from '../components/Accounts/AccountCard';

// Interface para o estado dos filtros (sem alterações)
interface FiltersState {
    accountId: string;
    searchText?: string;
    date?: string;
    amount?: number;
    page?: number;
    pageSize?: number;
}

export function HomePage() {
    // Estados existentes
    const [accounts, setAccounts] = useState<AccountResponseDto[]>([]);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [transactions, setTransactions] = useState<TransactionResponseDto[]>([]);
    const [activeFilters, setActiveFilters] = useState<FiltersState | null>(null);
    const [isLoadingAccounts, setIsLoadingAccounts] = useState(true);
    const [isLoadingTransactions, setIsLoadingTransactions] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [categories, setCategories] = useState<CategoryResponseDto[]>([]);
    const [transactionToEdit, setTransactionToEdit] = useState<TransactionResponseDto | null>(null);

    // Buscar as contas do usuário ao carregar a página (sem alterações)
    useEffect(() => {
        const loadInitialData = async () => {
            setIsLoadingAccounts(true);
            try {
                // Busca Contas e Categorias em paralelo
                const [accountsRes, categoriesRes] = await Promise.all([
                    accountService.getAllAccounts(),
                    categoryService.getAll()
                ]);

                setAccounts(accountsRes.data);
                setCategories(categoriesRes.data); // Salva as categorias
            } catch (error) {
                console.error("Erro ao carregar dados:", error);
                // Aqui você pode setar um estado de erro se tiver
            } finally {
                setIsLoadingAccounts(false);
            }
        };

        loadInitialData();
    }, []);

    // Buscar as transações QUANDO os filtros mudarem (sem alterações)
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

    // <<< Função para lidar com a criação de uma nova conta vinda do Modal >>>
    const handleAccountCreated = (newAccount: AccountResponseDto) => {
        // Adiciona a nova conta à lista existente e ordena alfabeticamente
        setAccounts(prevAccounts =>
            [...prevAccounts, newAccount].sort((a, b) => a.name.localeCompare(b.name))
        );
    };

    const handleEditAccount = (account: AccountResponseDto) => {
        console.log("Editar conta:", account);
        alert("Funcionalidade de Editar Conta será implementada no próximo passo!");
    };

    const handleDeleteAccount = (id: string) => {
        console.log("Excluir conta ID:", id);
        alert("Funcionalidade de Excluir Conta será implementada no próximo passo!");
    };

    const handleDeleteTransaction = async (id: string) => {
        try {
            // Chama o backend
            await transactionService.delete(id);
            setTransactions(prevTransactions => 
                prevTransactions.filter(tx => tx.id !== id)
            );
            alert('Transação excluída com sucesso!'); 

        } catch (err) {
            console.error("Erro ao deletar:", err);
            const axiosError = err as AxiosError<ApiErrorResponse>;
            alert(axiosError.response?.data?.message || "Erro ao tentar excluir a transação.");
        }
    };

    // Função para abrir o modal de NOVA transação (limpa o estado de edição)
    const handleOpenCreateModal = () => {
        setTransactionToEdit(null); 
        setIsModalOpen(true);
    };

    // Função para abrir o modal de EDIÇÃO (recebe a transação da lista)
    const handleOpenEditModal = (transaction: TransactionResponseDto) => {
        setTransactionToEdit(transaction);
        setIsModalOpen(true);
    };

    // Função para fechar o modal
    const handleCloseModal = () => {
        setIsModalOpen(false);
        setTransactionToEdit(null); // Limpa por segurança
        
        // Atualiza a lista para mostrar as mudanças (recarrega os filtros atuais)
        if (activeFilters) {
            setActiveFilters({ ...activeFilters });
        }
    };

    return (
        <div className="homepage-container">
            <Header />

            <main className="homepage-content">
                <h2>Contas e Transações</h2>

                {error && <div className="error-message">{error}</div>}

                {isLoadingAccounts ? (
                    <p>Carregando contas...</p>
                ) : (
                    <div className="accounts-grid">
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

                <div className="homepage-actions">
                    <CreateTransactionButton onClick={handleOpenCreateModal} />
                </div>

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

            {/* Modal para criar transação */}
            <TransactionModal
                accounts={accounts}
                categories={categories}
                isOpen={isModalOpen}
                onClose={handleCloseModal} // Use a função nova que criamos no passo 2
                onAccountCreated={handleAccountCreated}
                onCategoryCreated={handleCategoryCreated}
                transactionToEdit={transactionToEdit}
            />
        </div>
    );
}