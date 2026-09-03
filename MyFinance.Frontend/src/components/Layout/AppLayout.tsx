import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { MobileTabBar } from './MobileTabBar';
import './AppLayout.css';

/**
 * Casca das telas autenticadas.
 *
 * Antes, cada página importava e renderizava o `<Header />` por conta própria.
 * Centralizar aqui é o que permite trocar o header por uma barra de navegação
 * inferior no celular sem editar as quatro páginas, e é o único lugar que
 * precisa saber que essa barra existe (ver o `padding-bottom` em AppLayout.css).
 *
 * O `<main>` fica em cada página, não aqui: elas já têm o seu, com a largura de
 * container que lhes cabe.
 */
export function AppLayout() {
    return (
        <div className="app-shell">
            <a className="skip-link" href="#conteudo-principal">
                Pular para o conteúdo
            </a>
            <Header />
            <Outlet />
            <MobileTabBar />
        </div>
    );
}
