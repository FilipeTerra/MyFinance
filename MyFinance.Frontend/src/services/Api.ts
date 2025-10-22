import axios, { AxiosError } from 'axios';
import type { RegisterRequestDto } from '../types/RegisterRequestDto';
import type { LoginRequestDto } from '../types/LoginRequestDto';
import type { LoginResponseDto } from '../types/LoginResponseDto';

// Define a URL base da sua API .NET
// Use a URL HTTPS do seu launchSettings.json
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

// Cria uma instância do Axios
const apiClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

export interface ApiErrorResponse {
    message: string;
    errors?: Record<string, string[]>; // Para erros de validação do Model
}

// Funções para os endpoints de autenticação
const authService = {
    /**
     * Registra um novo usuário.
     * Corresponde a: POST /api/auth/register
     */
    register: (data: RegisterRequestDto) => {
        return apiClient.post('/auth/register', data);
    },

    /**
     * Autentica um usuário e retorna um token.
     * Corresponde a: POST /api/auth/login
     */
    login: (data: LoginRequestDto) => {
        // Especificamos que o tipo de retorno esperado é LoginResponseDto
        return apiClient.post<LoginResponseDto>('/auth/login', data);
    },

    // --- Futuras funções de API (ex: transações) podem ser adicionadas aqui ---
    // Ex:
    // setAuthToken: (token: string) => {
    //   apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
    // },
    // clearAuthToken: () => {
    //   delete apiClient.defaults.headers.common['Authorization'];
    // }
};

export default authService;
export { AxiosError };