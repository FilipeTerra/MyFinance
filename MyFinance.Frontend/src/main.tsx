import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
// Importe os componentes do react-router-dom
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';

import './index.css'; // Seus estilos globais

// Importe suas páginas (ajuste os caminhos se necessário)
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';

const rootElement = document.getElementById('root');
if (!rootElement) throw new Error('Failed to find the root element');
const root = createRoot(rootElement);

// Renderize a aplicação com as rotas configuradas
root.render(
    <StrictMode>
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<Navigate to="/login" replace />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage/>} />
            </Routes>
        </BrowserRouter>
    </StrictMode>,
);

