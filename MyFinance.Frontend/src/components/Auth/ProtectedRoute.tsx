import { Navigate, Outlet } from 'react-router-dom';
import { tokenManager } from '../../services/Api';

function isTokenExpired(token: string): boolean {
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return Date.now() / 1000 > payload.exp;
    } catch {
        return true;
    }
}

export function ProtectedRoute() {
    const token = localStorage.getItem('authToken');

    if (!token || isTokenExpired(token)) {
        tokenManager.clearAuthToken();
        localStorage.removeItem('authToken');
        return <Navigate to="/login" replace />;
    }

    tokenManager.setAuthToken(token);
    return <Outlet />;
}