import axios, { AxiosError } from 'axios';
import type { RegisterRequestDto } from '../types/RegisterRequestDto';
import type { LoginRequestDto } from '../types/LoginRequestDto';
import type { LoginResponseDto } from '../types/LoginResponseDto';
import type { AccountResponseDto } from '../types/AccountResponseDto';
import type { TransactionResponseDto } from '../types/TransactionResponseDto';
import type { TransactionRequestDto } from '../types/TransactionRequestDto';
import type { CategoryResponseDto } from '../types/CategoryResponseDto';
import type { AccountRequestDto } from '../types/AccountRequestDto';
import type { CategoryRequestDto } from '../types/CategoryRequestDto';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

const apiClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json; charset=utf-8',
    },
});

export interface ApiErrorResponse {
    message: string;
    errors?: Record<string, string[]>;
}

const tokenManager = {
    setAuthToken: (token: string | null) => {
        if (token) {
            apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
            console.log('Token set in Axios:', apiClient.defaults.headers.common['Authorization']);
        } else {
            delete apiClient.defaults.headers.common['Authorization'];
            console.log('Token removed from Axios.');
        }
    },
    clearAuthToken: () => {
        tokenManager.setAuthToken(null);
    }
};

// Funções para os endpoints de autenticação
const authService = {
    register: (data: RegisterRequestDto) => {
        return apiClient.post('/auth/register', data);
    },
    login: (data: LoginRequestDto) => {
        return apiClient.post<LoginResponseDto>('/auth/login', data);
    },
};

// Isso garante que se o usuário recarregar a página, o token ainda estará no Axios
const initialToken = localStorage.getItem('authToken');
tokenManager.setAuthToken(initialToken);

const accountService = {
    // Rota GET /api/accounts (do seu AccountController)
    getAllAccounts: () => {
        return apiClient.get<AccountResponseDto[]>('/accounts');
    },
    create: (data: AccountRequestDto) => {
        return apiClient.post<AccountResponseDto>('/accounts', data);
    }
};

const categoryService = {
    getAll: () => {
        return apiClient.get<CategoryResponseDto[]>('/categories');
    },
    create: (data: CategoryRequestDto) => {
        return apiClient.post<CategoryResponseDto>('/categories', data);
    }
};

// Interface para os parâmetros do filtro
// A interface TransactionFilterParams já corresponde ao TransactionSearchRequestDto do backend
export interface TransactionFilterParams {
    accountId: string;
    searchText?: string;
    date?: string; // Formato YYYY-MM-DD
    amount?: number;
    page?: number;
    pageSize?: number;
}

const transactionService = {
    getTransactions: (params: TransactionFilterParams) => {
        const filters = params;
        const queryParams = new URLSearchParams({
            accountId: filters.accountId,
            page: (filters.page || 1).toString(),
            pageSize: (filters.pageSize || 20).toString(),
        });

        if (filters.searchText) queryParams.append('searchText', filters.searchText);
        if (filters.date) queryParams.append('date', filters.date);
        if (filters.amount !== undefined && filters.amount !== null) queryParams.append('amount', filters.amount.toString());

        return apiClient.get<TransactionResponseDto[]>('/transactions/search', { params: queryParams });
    },
    create: (data: TransactionRequestDto) => {
        return apiClient.post<TransactionResponseDto>('/transactions', data);
    },
    update: (id: string, data: TransactionRequestDto) => {
        return apiClient.put<TransactionResponseDto>(`/transactions/${id}`, data);
    },
    delete: (id: string) => {
        return apiClient.delete<void>(`/transactions/${id}`);
    }
};

export {
    authService,
    accountService,
    categoryService,
    transactionService,
    tokenManager,
    AxiosError
};