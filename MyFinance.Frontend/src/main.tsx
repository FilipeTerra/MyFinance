import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
// Importe os componentes do react-router-dom
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';

import './index.css'; // Seus estilos globais

// Importe suas páginas (ajuste os caminhos se necessário)
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { HomePage } from "./pages/HomePage";
import { ProtectedRoute } from "./components/Auth/ProtectedRoute";

const rootElement = document.getElementById('root');
if (!rootElement) throw new Error('Failed to find the root element');
const root = createRoot(rootElement);

// Renderize a aplicação com as rotas configuradas
root.render(
    <StrictMode>
        <BrowserRouter>
            <Routes>
                {/* Rotas Públicas */}
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />

                {/* Rotas Protegidas */}
                <Route element={<ProtectedRoute />}> {/* <-- Envolve as rotas protegidas */}
                    <Route path="/home" element={<HomePage />} />

                    {/* Redireciona a raiz "/" para a home se estiver logado */}
                    <Route path="/" element={<Navigate to="/home" replace />} />
                </Route>

                {/* Rota Padrão (se nenhuma outra corresponder - opcional) */}
                {/* Pode redirecionar para login ou mostrar um 404 */}
                <Route path="*" element={<Navigate to="/login" replace />} />

            </Routes>   
        </BrowserRouter>
    </StrictMode>,
);

