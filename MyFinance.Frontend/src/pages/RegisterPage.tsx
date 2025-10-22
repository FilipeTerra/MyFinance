import { RegisterForm } from '../components/Auth/RegisterForm'; // Importa o componente do formulário
import './RegisterPage.css'; // Importa os estilos da PÁGINA de registro

export function RegisterPage() {
    return (
        // Container da PÁGINA para centralização
        <div className="register-page-container">
            {/* O Card que envolve o formulário */}
            <div className="register-page-card">
                {/* Renderiza o componente do formulário aqui dentro */}
                <RegisterForm />
            </div>
        </div>
    );
}

