import { NavLink } from 'react-router-dom';
import { useSessao } from '../../hooks/useSessao';
import './Header.css';

export function Header() {
    // A saudação e o "Sair" também aparecem no menu "Mais" do celular; a lógica
    // de encerrar sessão vive em useSessao para não divergir entre os dois.
    const { nome: userName, sair: handleLogout } = useSessao();

    return (
        <header className="app-header">
            <div className="header-logo">
                FinAI
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
                    to="/chat"
                    className={({ isActive }) => isActive ? "header-link active" : "header-link"}
                >
                    Consultor IA
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
                <span className="user-greeting">Olá, {userName}</span>
                <button onClick={handleLogout} className="logout-button">
                    Sair
                </button>
            </div>
        </header>
    );
}