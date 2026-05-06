import axios, { AxiosError } from 'axios';

const AI_API_BASE_URL = import.meta.env.VITE_AI_API_BASE_URL || 'http://localhost:8181/api/ai'; // Default para desenvolvimento

const aiApiClient = axios.create({
    baseURL: AI_API_BASE_URL,
    headers: {
        'Content-Type': 'application/json; charset=utf-8',
    },
    timeout: 30000, // 30 segundos timeout
});

export interface AiMessageResponse {
    message?: string;
    data?: any; // Para objetos complexos futuros (gráficos, tabelas)
}

export interface ApiErrorResponse {
    message: string;
    errors?: Record<string, string[]>;
}

/**
 * Envia uma mensagem para o agente de IA e retorna a resposta.
 * @param message - O texto da mensagem a ser enviada
 * @returns A resposta da API, que pode ser uma string ou um objeto complexo
 */
export async function sendMessage(message: string): Promise<AiMessageResponse> {
    try {
        const response = await aiApiClient.post<AiMessageResponse>('/chat', {
            prompt: message
        });

        const responseData = response.data as any;
        return {
            message: responseData.resposta ?? responseData.message,
            data: responseData.data,
            ...responseData,
        };
    } catch (error) {
        if (axios.isAxiosError(error)) {
            const axiosError = error as AxiosError<ApiErrorResponse>;
            throw new Error(
                axiosError.response?.data?.message ||
                axiosError.message ||
                'Erro ao comunicar com o agente de IA'
            );
        } else {
            throw new Error('Erro desconhecido ao enviar mensagem para IA');
        }
    }
}