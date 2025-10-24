import { Navigate, Outlet } from 'react-router-dom';
import { tokenManager } from '../../services/Api'; // Importa para garantir que o token seja verificado no Axios também

export function ProtectedRoute() {
    const token = localStorage.getItem('authToken');

    // Se não houver token no localStorage, redireciona para /login
    if (!token) {
        console.log("ProtectedRoute: No token found, redirecting to /login");
        tokenManager.clearAuthToken(); // Garante que o Axios também não tem token
        return <Navigate to="/login" replace />;
    }

    // Se houver token, garante que ele está configurado no Axios
    // (Isso ajuda se o usuário recarregar a página)
    tokenManager.setAuthToken(token);
    console.log("ProtectedRoute: Token found, rendering protected route.");

    // Renderiza o componente da rota solicitada (ex: HomePage)
    return <Outlet />;
}