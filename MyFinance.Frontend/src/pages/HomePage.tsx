import { useEffect, useState } from 'react';
import { Header } from '../components/Layout/Header';
import { accountService, transactionService, AxiosError, type ApiErrorResponse } from '../services/Api';
import type { AccountResponseDto } from '../types/AccountResponseDto';
import type { TransactionResponseDto } from '../types/TransactionResponseDto';
import { TransactionFilter } from '../components/Transactions/TransactionFilter';
import { TransactionList } from '../components/Transactions/TransactionList';
import './HomePage.css'; // (Novo CSS)

// Definindo a interface para o estado dos filtros
interface FiltersState {
    accountId: string;
    searchText?: string;
    date?: string;
    amount?: number;
    page?: number;
    pageSize?: number;
}

export function HomePage() {
    // Estado para a lista de contas (para o dropdown)
    const [accounts, setAccounts] = useState<AccountResponseDto[]>([]);

    // Estado para a lista de transaçíµes (o resultado)
    const [transactions, setTransactions] = useState<TransactionResponseDto[]>([]);

    // Estado para os filtros ativos
    const [activeFilters, setActiveFilters] = useState<FiltersState | null>(null);

    // Estados de loading
    const [isLoadingAccounts, setIsLoadingAccounts] = useState(true);
    const [isLoadingTransactions, setIsLoadingTransactions] = useState(false);

    // Estado de erro
    const [error, setError] = useState<string | null>(null);

    // Buscar as contas do usuários ao carregar a página
    useEffect(() => {
        const fetchAccounts = async () => {
            try {
                setError(null);
                setIsLoadingAccounts(true);
                const response = await accountService.getAllAccounts();
                setAccounts(response.data);
            } catch (err) {
                const axiosError = err as AxiosError<ApiErrorResponse>;
                setError(axiosError.response?.data?.message || 'Erro ao buscar contas.');
            } finally {
                setIsLoadingAccounts(false);
            }
        };
        fetchAccounts();
    }, []); // Roda apenas uma vez

    // Buscar as transações QUANDO os filtros mudarem
    useEffect(() => {
        // Não busca transações se nenhum filtro (conta) foi selecionado
        if (!activeFilters || !activeFilters.accountId) {
            setTransactions([]); // Limpa a lista se nenhuma conta estiver selecionada
            return;
        }

        const fetchTransactions = async () => {
            try {
                setError(null);
                setIsLoadingTransactions(true);
                // Passa os filtros para o serviço
                const response = await transactionService.getTransactions(activeFilters);
                setTransactions(response.data);
            } catch (err) {
                const axiosError = err as AxiosError<ApiErrorResponse>;
                setError(axiosError.response?.data?.message || 'Erro ao buscar transaçíµes.');
            } finally {
                setIsLoadingTransactions(false);
            }
        };

        fetchTransactions();

    }, [activeFilters]); // Roda toda vez que 'activeFilters' mudar

    // Função chamada pelo componente TransactionFilter
    const handleFilterChange = (filters: any) => {
        setActiveFilters({
            ...filters,
            page: 1, // Reseta para a página 1 a cada nova busca
            pageSize: 20
        });
    };

    return (
        <div className="homepage-container">
            <Header />

            <main className="homepage-content">
                <h2>Contas e Transações</h2>

                {/* Exibe erro, se houver */}
                {error && <div className="error-message">{error}</div>}

                {/* Seleção de Filtros */}
                {isLoadingAccounts ? (
                    <p>Carregando contas...</p>
                ) : (
                    <TransactionFilter
                        accounts={accounts}
                        onFilterChange={handleFilterChange}
                        isLoading={isLoadingTransactions}
                    />
                )}

                {/* Seleção da Lista */}
                <TransactionList
                    transactions={transactions}
                    isLoading={isLoadingTransactions}
                />
            </main>
        </div>
    );
}