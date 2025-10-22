import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import './RegisterForm.css';
import authService, { AxiosError, type ApiErrorResponse } from '../../services/Api';

export function RegisterForm() {
    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const navigate = useNavigate();

    const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setError(null);

        // Validação básica no frontend
        if (!name.trim() || !email.trim() || !password.trim() || !confirmPassword.trim()) {
            setError('Por favor, preencha todos os campos.');
            return;
        }
        if (password !== confirmPassword) {
            setError('As senhas não coincidem.');
            return;
        }
        if (password.length < 6) {
            setError('A senha deve ter pelo menos 6 caracteres.');
            return;
        }

        setIsLoading(true);

        try {
            // Chama a API de registro
            await authService.register({ name, email, password, confirmPassword });

            // Sucesso! Redireciona para /login com uma mensagem
            navigate('/login?registration=success');

        } catch (err) {
            // Trata erros da API
            const axiosError = err as AxiosError<ApiErrorResponse>;
            const apiErrorMessage = axiosError.response?.data?.message;

            if (apiErrorMessage) {
                setError(apiErrorMessage); // Ex: "Este email já está cadastrado."
            } else if (axiosError.request) {
                setError('Não foi possível conectar ao servidor.');
            } else {
                setError('Erro inesperado. Tente novamente.');
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="register-form" noValidate>
            <h2>Crie sua Conta</h2> {/* Título */}

            {/* Exibição de mensagens de erro */}
            {error && <p className="error-message">{error}</p>}

            {/* Grupo para Nome */}
            <div className="form-group">
                <label htmlFor="register-name">Nome Completo</label>
                <input
                    type="text" id="register-name" value={name}
                    onChange={(e) => setName(e.target.value)}
                    required placeholder="Seu nome completo" aria-label="Nome Completo"
                    disabled={isLoading}
                />
            </div>

            {/* Grupo para Email */}
            <div className="form-group">
                <label htmlFor="register-email">E-mail</label>
                <input
                    type="email" id="register-email" value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required placeholder="seuemail@exemplo.com" aria-label="Endereço de e-mail"
                    disabled={isLoading}
                />
            </div>

            {/* Grupo para Senha */}
            <div className="form-group">
                <label htmlFor="register-password">Senha</label>
                <input
                    type="password" id="register-password" value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required placeholder="Mínimo 6 caracteres" aria-label="Senha"
                    minLength={6} disabled={isLoading}
                />
            </div>

            {/* Grupo para Confirmar Senha */}
            <div className="form-group">
                <label htmlFor="register-confirm-password">Confirme sua Senha</label>
                <input
                    type="password" id="register-confirm-password" value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required placeholder="Repita a senha" aria-label="Confirmação da Senha"
                    minLength={6} disabled={isLoading}
                />
            </div>

            {/* Botão de submissão */}
            <button type="submit" className="register-button" disabled={isLoading}>
                {isLoading ? 'Cadastrando...' : 'Cadastrar'}
            </button>

            {/* Link para Login */}
            <div className="register-links">
                <p>Já tem uma conta? <Link to="/login">Faça login</Link></p>
            </div>
        </form>
    );
}

