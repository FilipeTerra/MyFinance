import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { tokenManager } from '../services/Api';

/**
 * Dados e ações da sessão do usuário.
 *
 * Extraído do `Header` porque a navegação mobile precisa exatamente da mesma
 * saudação e do mesmo "Sair": no celular o header some e essas duas coisas
 * passam a viver no menu "Mais". Duplicar a limpeza de sessão em dois lugares
 * é como se esquece de limpar o token do Axios em um deles.
 */
export function useSessao() {
    const navigate = useNavigate();

    const nome = localStorage.getItem('userName') || 'Usuário';

    const sair = useCallback(() => {
        localStorage.removeItem('authToken');
        localStorage.removeItem('userName');
        tokenManager.clearAuthToken();
        navigate('/login');
    }, [navigate]);

    return { nome, sair };
}
