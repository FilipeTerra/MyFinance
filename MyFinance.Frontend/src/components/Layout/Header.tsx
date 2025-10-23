import { NavLink, useNavigate } from 'react-router-dom';
import { tokenManager } from '../../services/Api'; // Importa o tokenManager
import './Header.css'; // Criaremos este CSS a seguir

export function Header() {
    const navigate = useNavigate();
    // Pega o nome do usu�rio do localStorage, com um fallback
    const userName = localStorage.getItem('userName') || 'Usu�rio';

    const handleLogout = () => {
        localStorage.removeItem('authToken');
        localStorage.removeItem('userName');
        tokenManager.clearAuthToken(); // Limpa o token do Axios
        navigate('/login'); // Redireciona para a p�gina de login
    };

    return (
        <header className="app-header">
            <div className="header-logo">
                {/* Voc� pode adicionar um link para a home aqui se quiser */}
                MyFinance
            </div>
            <nav className="header-nav">
                {/* Usamos NavLink para que ele adicione a classe 'active' automaticamente */}
                <NavLink
                    to="/home"
                    className={({ isActive }) => isActive ? "header-link active" : "header-link"}
                >
                    Home
                </NavLink>
                <NavLink
                    to="/dashboard"
                    className={({ isActive }) => isActive ? "header-link active" : "header-link"}
                >
                    Dashboard
                </NavLink>
                <NavLink
                    to="/profile"
                    className={({ isActive }) => isActive ? "header-link active" : "header-link"}
                >
                    Perfil
                </NavLink>
            </nav>
            <div className="header-user">
                <span className="user-greeting">Ol�, {userName}</span>
                <button onClick={handleLogout} className="logout-button">
                    Sair
                </button>
            </div>
        </header>
    );
}