import axios, { AxiosError } from 'axios';
import type { RegisterRequestDto } from '../types/RegisterRequestDto';
import type { LoginRequestDto } from '../types/LoginRequestDto';
import type { LoginResponseDto } from '../types/LoginResponseDto';
import type { AccountResponseDto } from '../types/AccountResponseDto';
import type { TransactionResponseDto } from '../types/TransactionResponseDto';

// Define a URL base da sua API .NET
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

// Cria uma instância do Axios
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

// --- Funçíµes de gerenciamento do Token ---
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

// Funçíµes para os endpoints de autenticação
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
};

// Interface para os parâmetros do filtro
export interface TransactionFilterParams {
    accountId: string;
    searchText?: string;
    date?: string; // Formato YYYY-MM-DD
    amount?: number;
    page?: number;
    pageSize?: number;
}

const transactionService = {
    // Rota GET /api/transactions/account/{accountId}?searchText=...&page=...
    getTransactions: (params: TransactionFilterParams) => {
        const { accountId, ...filters } = params;

        // Constrói os query params
        const queryParams = new URLSearchParams({
            page: (filters.page || 1).toString(),
            pageSize: (filters.pageSize || 20).toString(),
        });

        if (filters.searchText) queryParams.append('searchText', filters.searchText);
        if (filters.date) queryParams.append('date', filters.date);
        if (filters.amount) queryParams.append('amount', filters.amount.toString());

        // O endpoint do backend esperado é: GET /api/transactions/account/{accountId}?[QUERY_PARAMS]
        return apiClient.get<TransactionResponseDto[]>(`/transactions/account/${accountId}`, { params: queryParams });
    },
};

export {
    authService,
    accountService,
    transactionService,
    tokenManager,
    AxiosError
};