import axios, { AxiosError } from 'axios';
import type { RegisterRequestDto } from '../types/RegisterRequestDto';
import type { LoginRequestDto } from '../types/LoginRequestDto';
import type { LoginResponseDto } from '../types/LoginResponseDto';

// Define a URL base da sua API .NET
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

// Cria uma inst�ncia do Axios
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

// --- Fun��es de gerenciamento do Token ---
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

// Fun��es para os endpoints de autentica��o
const authService = {
    register: (data: RegisterRequestDto) => {
        return apiClient.post('/auth/register', data);
    },
    login: (data: LoginRequestDto) => {
        return apiClient.post<LoginResponseDto>('/auth/login', data);
    },
};

// --- Futuramente, adicione outros servi�os aqui (accountService, transactionService) ---
// const accountService = { ... };

// Exporta os servi�os e helpers necess�rios
export { authService, tokenManager, AxiosError };

// --- B�NUS: Configurar o token ao carregar a aplica��o ---
// Isso garante que se o usu�rio recarregar a p�gina, o token ainda estar� no Axios
const initialToken = localStorage.getItem('authToken');
tokenManager.setAuthToken(initialToken);