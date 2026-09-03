import { useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { MobileMoreSheet } from './MobileMoreSheet';
import { IconeHome, IconeConsultor, IconeDashboard, IconeMais } from './navIcons';
import './MobileTabBar.css';

/**
 * Navegação principal no celular: barra fixa no rodapé, na zona do polegar.
 *
 * Fica sempre montada e é o CSS que decide se aparece (ver MobileTabBar.css);
 * acima de 768px quem aparece é o `Header`. `display: none` já tira o elemento
 * da árvore de acessibilidade e da ordem de tabulação, então as duas navegações
 * nunca coexistem para quem usa leitor de tela — e não há o piscar de primeira
 * renderização que um `matchMedia` traria.
 *
 * Os rótulos são idênticos aos do header de propósito: o mesmo destino não deve
 * mudar de nome conforme a largura da tela.
 */

const DESTINOS = [
    { to: '/home', rotulo: 'Home', Icone: IconeHome },
    { to: '/chat', rotulo: 'Consultor IA', Icone: IconeConsultor },
    { to: '/dashboard', rotulo: 'Dashboard', Icone: IconeDashboard },
];

export function MobileTabBar() {
    const [menuAberto, setMenuAberto] = useState(false);
    const { pathname } = useLocation();

    // "Mais" é o caminho até o Perfil, então acende junto — mas só visualmente.
    // O `aria-current="page"` de verdade fica no link do Perfil dentro do menu.
    const maisAtivo = menuAberto || pathname.startsWith('/profile');

    return (
        <>
            <nav className="mtab-bar" aria-label="Navegação principal">
                <ul className="mtab-list">
                    {DESTINOS.map(({ to, rotulo, Icone }) => (
                        <li key={to} className="mtab-item">
                            {/* NavLink já emite aria-current="page" quando ativo. */}
                            <NavLink
                                to={to}
                                className={({ isActive }) =>
                                    `mtab-link${isActive ? ' mtab-link--ativo' : ''}`
                                }
                            >
                                <span className="mtab-icone" aria-hidden="true">
                                    <Icone />
                                </span>
                                <span className="mtab-rotulo">{rotulo}</span>
                            </NavLink>
                        </li>
                    ))}

                    <li className="mtab-item">
                        <button
                            type="button"
                            className={`mtab-link${maisAtivo ? ' mtab-link--ativo' : ''}`}
                            aria-haspopup="dialog"
                            aria-expanded={menuAberto}
                            aria-controls="menu-mais"
                            onClick={() => setMenuAberto(true)}
                        >
                            <span className="mtab-icone" aria-hidden="true">
                                <IconeMais />
                            </span>
                            <span className="mtab-rotulo">Mais</span>
                        </button>
                    </li>
                </ul>
            </nav>

            <MobileMoreSheet aberto={menuAberto} onFechar={() => setMenuAberto(false)} />
        </>
    );
}
