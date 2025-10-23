import axios, { AxiosError } from 'axios';
import type { RegisterRequestDto } from '../types/RegisterRequestDto';
import type { LoginRequestDto } from '../types/LoginRequestDto';
import type { LoginResponseDto } from '../types/LoginResponseDto';

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

// --- Funções de gerenciamento do Token ---
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

// --- Futuramente, adicione outros serviços aqui (accountService, transactionService) ---
// const accountService = { ... };

// Exporta os serviços e helpers necessários
export { authService, tokenManager, AxiosError };

// --- BÔNUS: Configurar o token ao carregar a aplicação ---
// Isso garante que se o usuário recarregar a página, o token ainda estará no Axios
const initialToken = localStorage.getItem('authToken');
tokenManager.setAuthToken(initialToken);