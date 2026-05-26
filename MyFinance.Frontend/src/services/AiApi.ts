import axios, { AxiosError } from 'axios';

const AI_API_BASE_URL = import.meta.env.VITE_AI_API_BASE_URL || 'http://localhost:8181/api/ai';

const aiApiClient = axios.create({
    baseURL: AI_API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 300000, // 5 minutos — Ollama pode levar bastante tempo para responder
});

export interface AiMessageResponse {
    message?: string;
    data?: any;
}

export interface ApiErrorResponse {
    message: string;
    errors?: Record<string, string[]>;
}

export class SessionExpiredError extends Error {
    constructor() {
        super('session_expired');
        this.name = 'SessionExpiredError';
    }
}

export interface LearnRule {
    description: string;
    categoryName: string;
}

export async function learnFromBatch(accountId: string, rules: LearnRule[]): Promise<void> {
    try {
        await aiApiClient.post('/learn', {
            account_id: accountId,
            rules: rules.map(r => ({
                description: r.description,
                category_name: r.categoryName,
            })),
        });
    } catch (error) {
        // Falha no aprendizado não pode bloquear o fluxo principal
        console.warn('[KB] Falha ao persistir aprendizado (non-blocking):', error);
    }
}

export async function sendMessage(message: string): Promise<AiMessageResponse> {
    const jwtToken = localStorage.getItem('authToken');
    if (!jwtToken) throw new SessionExpiredError();

    try {
        const response = await aiApiClient.post('/chat', {
            jwt_token: jwtToken,
            prompt: message,
        });

        const responseData = response.data as any;

        if (!responseData.success && responseData.error_type === 'session_expired') {
            localStorage.removeItem('authToken');
            throw new SessionExpiredError();
        }

        return {
            message: responseData.resposta ?? responseData.message ?? 'Sem resposta do agente.',
            data: responseData.data,
        };
    } catch (error) {
        if (error instanceof SessionExpiredError) throw error;

        if (axios.isAxiosError(error)) {
            const axiosError = error as AxiosError<any>;
            if (axiosError.code === 'ECONNABORTED') {
                throw new Error('O agente demorou muito para responder. Tente novamente.');
            }
            const serverMessage =
                axiosError.response?.data?.detail ||
                axiosError.response?.data?.message ||
                axiosError.message;
            throw new Error(serverMessage || 'Erro ao comunicar com o agente de IA.');
        }
        throw new Error('Erro desconhecido ao enviar mensagem para IA.');
    }
}
