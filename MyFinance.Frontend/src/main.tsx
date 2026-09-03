import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
// Importe os componentes do react-router-dom
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';

import './index.css'; // Seus estilos globais

// Importe suas páginas (ajuste os caminhos se necessário)
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { HomePage } from "./pages/HomePage";
import { ChatPage } from "./pages/ChatPage";
import { DashboardPage } from "./pages/DashboardPage";
import { ProfilePage } from "./pages/ProfilePage";
import { ProtectedRoute } from "./components/Auth/ProtectedRoute";
import { AppLayout } from "./components/Layout/AppLayout";

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
                    {/* Fora do AppLayout: é só um redirecionamento, não deve
                        montar header e barra de navegação por um quadro. */}
                    <Route path="/" element={<Navigate to="/home" replace />} />

                    {/* AppLayout desenha o header (desktop) e a barra de
                        navegação inferior (celular) ao redor de cada página. */}
                    <Route element={<AppLayout />}>
                        <Route path="/home" element={<HomePage />} />
                        <Route path="/chat" element={<ChatPage />} />
                        <Route path="/dashboard" element={<DashboardPage />} />
                        <Route path="/profile" element={<ProfilePage />} />
                    </Route>
                </Route>

                {/* Rota Padrão (se nenhuma outra corresponder - opcional) */}
                {/* Pode redirecionar para login ou mostrar um 404 */}
                <Route path="*" element={<Navigate to="/login" replace />} />

            </Routes>   
        </BrowserRouter>
    </StrictMode>,
);

