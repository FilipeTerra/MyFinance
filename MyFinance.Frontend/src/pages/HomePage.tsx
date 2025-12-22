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

    return (
        <div className="homepage-container">
            <Header />

            <main className="homepage-content">
                <h2>Contas e Transações</h2>

                {error && <div className="error-message">{error}</div>}

                <div className="homepage-actions">
                    <CreateTransactionButton onClick={() => setIsModalOpen(true)} />
                </div>

                {isLoadingAccounts ? (
                    <p>Carregando contas...</p>
                ) : (
                    <TransactionFilter
                        accounts={accounts} // Passa a lista atualizada
                        onFilterChange={handleFilterChange}
                        isLoading={isLoadingTransactions}
                    />
                )}

                <TransactionList
                    transactions={transactions}
                    isLoading={isLoadingTransactions}
                    onDelete={handleDeleteTransaction}
                />
            </main>

            {/* Modal para criar transação */}
            <TransactionModal
                accounts={accounts}
                categories={categories} // Passa a lista
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                onAccountCreated={handleAccountCreated}
                onCategoryCreated={handleCategoryCreated} // Passa a função de atualização
            />
        </div>
    );
}