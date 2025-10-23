import React, { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import './LoginForm.css'; // Estilos específicos do formulário
import {authService, AxiosError, tokenManager, type ApiErrorResponse } from '../../services/Api';

export function LoginForm() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    const navigate = useNavigate(); // Hook para redirecionar o usuário
    const location = useLocation(); // Hook para ler parâmetros da URL

    useEffect(() => {
        const params = new URLSearchParams(location.search);
        if (params.get('registration') === 'success') {
            setSuccessMessage('Usuário cadastrado com sucesso! Faça seu login.');
            // Limpa o parâmetro da URL para não mostrar a mensagem novamente
            window.history.replaceState({}, document.title, window.location.pathname);
        }
    }, [location.search]);

    const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setError(null);
        setSuccessMessage(null); // Limpa msg de sucesso ao tentar logar

        if (!email.trim() || !password.trim()) {
            setError('Por favor, preencha o e-mail e a senha.');
            return;
        }

        setIsLoading(true);

        try {
            // Chama a função de login do nosso serviço
            const response = await authService.login({ email, password });

            const { token, userName, userEmail } = response.data;
            console.log('Login bem-sucedido!', { token, userName, userEmail });

            // Armazenar o token JWT
            localStorage.setItem('authToken', token);
            localStorage.setItem('userName', userName); // Salva o nome para usar no dashboard

            tokenManager.setAuthToken(token); // Configura o token no Axios

            // Redirecionar para a página principal (Dashboard)
            navigate('/home');

        } catch (err) {
            // CRITÉRIO DE ACEITAÇÃO: Se o email ou senha estiverem incorretos...
            const axiosError = err as AxiosError<ApiErrorResponse>;
            const apiErrorMessage = axiosError.response?.data?.message;

            if (apiErrorMessage) {
                setError(apiErrorMessage); // Ex: "Email ou senha inválidos."
            } else if (axiosError.request) {
                setError('Não foi possível conectar ao servidor. Verifique sua conexão.');
            } else {
                setError('Erro inesperado. Tente novamente.');
            }
        } finally {
            setIsLoading(false); // Para o loading em qualquer caso
        }
    };

    return (
        // O formulário em si, sem o card externo
        <form onSubmit={handleSubmit} className="login-form" noValidate>
            <h2>Acesse sua Conta</h2> {/* Título dentro do form */}
            {/* Mensagem de Sucesso (vinda do registro) */}
            {successMessage && <p className="success-message">{successMessage}</p>}

            {/* Mensagem de Erro (do login) */}
            {error && <p className="error-message">{error}</p>}

            {/* Grupo para Email */}
            <div className="form-group">
                <label htmlFor="login-email">E-mail</label>
                <input
                    type="email" id="login-email" value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required placeholder="seuemail@exemplo.com" aria-label="Endereço de e-mail"
                    disabled={isLoading} // Desabilita o campo durante o loading
                />
            </div>

            {/* Grupo para Senha */}
            <div className="form-group">
                <label htmlFor="login-password">Senha</label>
                <input
                    type="password" id="login-password" value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required placeholder="Digite sua senha" aria-label="Senha"
                    disabled={isLoading}
                />
            </div>

            {/* Botão de submissão */}
            <button type="submit" className="login-button" disabled={isLoading}>
                {isLoading ? 'Entrando...' : 'Entrar'} {/* Muda o texto do botão */}
            </button>

            {/* Links adicionais */}
            <div className="login-links">
                <p>Novo(a) aqui? <Link to="/register">Cadastre-se</Link></p>
                <p><Link to="/forgot-password">Esqueceu sua senha?</Link></p>
            </div>
        </form>
    );
}

