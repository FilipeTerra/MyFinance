import { useEffect, useState } from 'react';
import { Header } from '../components/Layout/Header';
// <<< ADICIONADO: Importar accountService para criação de conta >>>
import { accountService, transactionService, AxiosError, type ApiErrorResponse } from '../services/Api';
import type { AccountResponseDto } from '../types/AccountResponseDto';
import type { TransactionResponseDto } from '../types/TransactionResponseDto';
import { TransactionFilter } from '../components/Transactions/TransactionFilter';
import { TransactionList } from '../components/Transactions/TransactionList';
import { CreateTransactionButton } from '../components/Transactions/CreateTransactionButton';
import { TransactionModal } from '../components/Transactions/TransactionModal';
import './HomePage.css';

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

    // Buscar as contas do usuário ao carregar a página (sem alterações)
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

    // Função chamada pelo componente TransactionFilter (sem alterações)
    const handleFilterChange = (filters: any) => {
        setActiveFilters({
            ...filters,
            page: 1,
            pageSize: 20
        });
    };

    // <<< ADICIONADO: Função para lidar com a criação de uma nova conta vinda do Modal >>>
    const handleAccountCreated = (newAccount: AccountResponseDto) => {
        // Adiciona a nova conta à lista existente e ordena alfabeticamente
        setAccounts(prevAccounts =>
            [...prevAccounts, newAccount].sort((a, b) => a.name.localeCompare(b.name))
        );
        // O modal já seleciona a nova conta automaticamente, então não precisamos fazer nada aqui
        // Poderia adicionar uma mensagem de sucesso temporária se desejado
    };

    // <<< ADICIONADO: Função para lidar com a criação de uma nova transação vinda do Modal >>>
    // Esta função será chamada quando implementarmos a criação real da transação
    // const handleTransactionCreated = (newTransaction: TransactionResponseDto) => {
    // TODO: Atualizar a lista de transações (se a conta da nova transação for a selecionada no filtro)
    // ou talvez re-buscar as transações para a conta atual
    // console.log("Nova transação criada:", newTransaction);
    // Exemplo simples: rebuscar se a conta for a mesma
    // if (activeFilters && newTransaction.accountId === activeFilters.accountId) {
    //     // Trigger useEffect para recarregar transações
    //     setActiveFilters({...activeFilters}); // Pode precisar de uma forma mais robusta de trigger
    // }
    // };

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
                />
            </main>

            {/* Modal para criar transação */}
            <TransactionModal
                accounts={accounts} // Passa a lista atualizada
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                // <<< Passa a função handleAccountCreated >>>
                onAccountCreated={handleAccountCreated}
            // <<< Passaria a função para atualizar transações >>>
            // onTransactionCreated={handleTransactionCreated}
            />
        </div>
    );
}