import { useRef } from 'react';
import { createPortal } from 'react-dom';
import { NavLink } from 'react-router-dom';
import { useSessao } from '../../hooks/useSessao';
import { useDialogBehavior } from '../../hooks/useDialogBehavior';
import { IconePerfil, IconeSair } from './navIcons';
import './MobileMoreSheet.css';

interface MobileMoreSheetProps {
    aberto: boolean;
    onFechar: () => void;
}

/**
 * Menu "Mais" da navegação mobile: o que não cabe na barra inferior.
 *
 * Perfil e Sair são as ações de menor frequência do app, então descem um nível
 * em vez de ocupar espaço permanente na zona do polegar.
 *
 * `fecharAcimaDe: 768` fecha o menu ao girar para paisagem ou redimensionar a
 * janela: acima de 768px a barra que o abriu deixa de existir, e sem isso
 * sobraria um diálogo invisível com a rolagem do fundo travada.
 */
export function MobileMoreSheet({ aberto, onFechar }: MobileMoreSheetProps) {
    const painelRef = useRef<HTMLDivElement>(null);
    const { nome, sair } = useSessao();

    useDialogBehavior({ aberto, onFechar, painelRef, fecharAcimaDe: 768 });

    if (!aberto) return null;

    return createPortal(
        <div
            className="msheet-overlay"
            /* mousedown + identidade do alvo, e não click no overlay com
               stopPropagation no painel: com click, selecionar um texto dentro
               do painel e soltar o mouse fora fecharia o menu. */
            onMouseDown={(evento) => {
                if (evento.target === evento.currentTarget) onFechar();
            }}
        >
            <div
                ref={painelRef}
                id="menu-mais"
                className="msheet"
                role="dialog"
                aria-modal="true"
                aria-label="Mais opções"
                tabIndex={-1}
            >
                <div className="msheet-alca" aria-hidden="true" />

                <p className="msheet-saudacao">Olá, {nome}</p>

                <div className="msheet-lista">
                    <NavLink to="/profile" className="msheet-item" onClick={onFechar}>
                        <IconePerfil />
                        Perfil
                    </NavLink>

                    <button
                        type="button"
                        className="msheet-item msheet-item--sair"
                        onClick={() => {
                            onFechar();
                            sair();
                        }}
                    >
                        <IconeSair />
                        Sair
                    </button>
                </div>
            </div>
        </div>,
        document.body,
    );
}
