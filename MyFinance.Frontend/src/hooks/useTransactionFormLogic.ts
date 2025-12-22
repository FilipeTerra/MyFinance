import { useState } from 'react';
import { transactionService, accountService, categoryService, AxiosError } from '../services/Api';
import type { TransactionRequestDto } from '../types/TransactionRequestDto';
import type { AccountRequestDto } from '../types/AccountRequestDto';
import type { CategoryRequestDto } from '../types/CategoryRequestDto';

export function useTransactionFormLogic() {
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const createTransaction = async (data: TransactionRequestDto) => {
        setIsLoading(true);
        setError(null);
        try {
            const response = await transactionService.create(data);
            return response.data;
        } catch (err) {
            const axiosError = err as AxiosError<{ message: string }>;
            setError(axiosError.response?.data?.message || 'Erro ao criar transação.');
            throw err;
        } finally {
            setIsLoading(false);
        }
    };

    const updateTransaction = async (id: string, data: TransactionRequestDto) => {
        setIsLoading(true);
        setError(null);
        try {
            // Chama o método update que criamos no Passo 1
            const response = await transactionService.update(id, data);
            return response.data;
        } catch (err) {
            const axiosError = err as AxiosError<{ message: string }>;
            setError(axiosError.response?.data?.message || 'Erro ao atualizar transação.');
            throw err;
        } finally {
            setIsLoading(false);
        }
    };

    const createAccount = async (data: AccountRequestDto) => {
        setIsLoading(true); // Opcional: pode usar um estado de loading separado se preferir
        setError(null);
        try {
            const response = await accountService.create(data);
            return response.data;
        } catch (err) {
            const axiosError = err as AxiosError<{ message: string }>;
            setError(axiosError.response?.data?.message || 'Erro ao criar conta.');
            throw err;
        } finally {
            setIsLoading(false);
        }
    };

    const createCategory = async (data: CategoryRequestDto) => {
        setIsLoading(true);
        setError(null);
        try {
            const response = await categoryService.create(data);
            return response.data;
        } catch (err) {
            const axiosError = err as AxiosError<{ message: string }>;
            setError(axiosError.response?.data?.message || 'Erro ao criar categoria.');
            throw err;
        } finally {
            setIsLoading(false);
        }
    };

    return {
        isLoading,
        error,
        createTransaction,
        updateTransaction,
        createAccount,
        createCategory,
        setError
    };
}