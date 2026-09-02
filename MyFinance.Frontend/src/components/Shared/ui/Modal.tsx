import { useId, useRef } from 'react';
import { createPortal } from 'react-dom';
import type { ReactNode, RefObject } from 'react';
import { useDialogBehavior } from '../../../hooks/useDialogBehavior';
import './Modal.css';

export type TamanhoModal = 'sm' | 'md' | 'lg' | 'xl';

export interface ModalProps {
    /** Se o pai já faz `{aberto && <Modal/>}`, pode omitir. */
    aberto?: boolean;
    onFechar: () => void;
    /** Título acessível: vira o <h2> do cabeçalho e o alvo do aria-labelledby. */
    titulo: ReactNode;
    /** sm 400 | md 480 | lg 560 | xl 960. No celular todos ocupam a largura toda. */
    tamanho?: TamanhoModal;
    children: ReactNode;
    /** Barra de ações colada no rodapé. Só use quando os botões NÃO estiverem
     *  dentro de um <form> no children — tirar um submit de dentro do form
     *  quebra a associação com ele. */
    rodape?: ReactNode;
    /** Cabeçalho e rodapé fixos, corpo rolando por dentro. Padrão: o painel
     *  inteiro cresce e quem rola é o overlay. */
    corpoRolavel?: boolean;
    /** Padrão true. Desligue em formulários longos com dados não salvos. */
    fecharNoFundo?: boolean;
    /** Sem o cabeçalho padrão — para modais que desenham o próprio topo. */
    semCabecalho?: boolean;
    /** Ponte para o CSS de cada modal; aplicada ao painel. */
    className?: string;
    /** Elemento a focar ao abrir. Padrão: o próprio painel. */
    focoInicialRef?: RefObject<HTMLElement | null>;
}

/**
 * Base compartilhada dos modais do app.
 *
 * Substitui cinco dialetos que redefiniam overlay e painel por conta própria —
 * dois deles declarando globalmente as *mesmas* classes `.modal-overlay` e
 * `.modal-content` com larguras diferentes, de modo que quem vencia dependia da
 * ordem de import.
 *
 * Além de unificar, conserta de uma vez o que faltava em todos: rolar quando o
 * conteúdo é mais alto que a tela, fechar no Escape, prender o foco, devolver o
 * foco ao fechar e travar a rolagem do fundo.
 */
export function Modal({
    aberto = true,
    onFechar,
    titulo,
    tamanho = 'md',
    children,
    rodape,
    corpoRolavel = false,
    fecharNoFundo = true,
    semCabecalho = false,
    className,
    focoInicialRef,
}: ModalProps) {
    const painelRef = useRef<HTMLDivElement>(null);
    const tituloId = useId();

    useDialogBehavior({ aberto, onFechar, painelRef, focoInicialRef });

    if (!aberto) return null;

    const classes = [
        'ui-modal',
        `ui-modal--${tamanho}`,
        corpoRolavel && 'ui-modal--corpo-rolavel',
        className,
    ]
        .filter(Boolean)
        .join(' ');

    return createPortal(
        <div
            className="ui-modal-overlay"
            /* mousedown + identidade do alvo em vez de click no overlay com
               stopPropagation no painel: com click, selecionar texto dentro do
               painel e soltar o mouse fora fecharia o modal. */
            onMouseDown={(evento) => {
                if (fecharNoFundo && evento.target === evento.currentTarget) onFechar();
            }}
        >
            <div
                ref={painelRef}
                className={classes}
                role="dialog"
                aria-modal="true"
                aria-labelledby={semCabecalho ? undefined : tituloId}
                aria-label={semCabecalho && typeof titulo === 'string' ? titulo : undefined}
                tabIndex={-1}
            >
                {!semCabecalho && (
                    <div className="ui-modal-cabecalho">
                        <h2 id={tituloId} className="ui-modal-titulo">{titulo}</h2>
                        <button
                            type="button"
                            className="ui-modal-fechar"
                            onClick={onFechar}
                            aria-label="Fechar"
                        >
                            ×
                        </button>
                    </div>
                )}

                <div className="ui-modal-corpo">{children}</div>

                {rodape && <div className="ui-modal-rodape">{rodape}</div>}
            </div>
        </div>,
        document.body,
    );
}
