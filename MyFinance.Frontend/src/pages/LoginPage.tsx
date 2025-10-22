import { LoginForm } from '../components/Auth/LoginForm'; // Importa o componente do formulário
import './LoginPage.css'; // Importa os estilos da PÁGINA

export function LoginPage() {
    return (
        // Container da PÁGINA para centralização
        <div className="login-page-container">
            {/* O Card que envolve o formulário */}
            <div className="login-page-card">
                {/* Renderiza o componente do formulário aqui dentro */}
                <LoginForm />
            </div>
        </div>
    );
}

