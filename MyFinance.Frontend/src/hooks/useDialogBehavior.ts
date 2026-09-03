import { useEffect, useRef } from 'react';
import type { RefObject } from 'react';

/**
 * Comportamento compartilhado de diálogo modal: Escape, foco preso, foco
 * devolvido e trava de rolagem do fundo.
 *
 * Usado pelo menu "Mais" da navegação mobile e pelo `Modal` de `Shared/ui`.
 * Antes deste hook, nenhum dos dez modais do app fechava no Escape nem prendia
 * o foco — dava para sair do modal com Tab e interagir com a página atrás dele.
 */

const SELETOR_FOCAVEL = [
    'a[href]',
    'button:not([disabled])',
    'input:not([disabled])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    '[tabindex]:not([tabindex="-1"])',
].join(', ');

/* ── Trava de rolagem do body ───────────────────────────────────────────────
   Contagem de referência: se um modal abrir por cima do menu "Mais", fechar o
   de cima não pode destravar a rolagem enquanto o de baixo continua aberto. */
let travas = 0;
let overflowAnterior = '';
let paddingAnterior = '';

function travarBody() {
    if (travas === 0) {
        // No desktop, esconder a barra de rolagem encolhe a viewport e a página
        // "pula" para o lado; o padding compensa a largura dela.
        const larguraBarra = window.innerWidth - document.documentElement.clientWidth;
        overflowAnterior = document.body.style.overflow;
        paddingAnterior = document.body.style.paddingRight;
        document.body.style.overflow = 'hidden';
        if (larguraBarra > 0) {
            document.body.style.paddingRight = `${larguraBarra}px`;
        }
    }
    travas += 1;
}

function destravarBody() {
    travas = Math.max(0, travas - 1);
    if (travas === 0) {
        document.body.style.overflow = overflowAnterior;
        document.body.style.paddingRight = paddingAnterior;
    }
}

/* ── Pilha de diálogos abertos ──────────────────────────────────────────────
   Só o diálogo do topo responde ao Escape. Sem isso, dois diálogos abertos
   fechariam juntos — os dois ouvem `document` e ambos veriam a mesma tecla. */
const pilha: symbol[] = [];

interface UseDialogBehaviorOptions {
    aberto: boolean;
    onFechar: () => void;
    /** Painel do diálogo: recebe o foco inicial e delimita o foco preso. */
    painelRef: RefObject<HTMLElement | null>;
    /** Elemento a focar ao abrir, no lugar do próprio painel. */
    focoInicialRef?: RefObject<HTMLElement | null>;
    /** Fecha sozinho ao passar desta largura em px. Usado pelo menu "Mais",
     *  que só existe no mobile: girar para paisagem com ele aberto deixaria um
     *  diálogo invisível e o body travado. */
    fecharAcimaDe?: number;
}

export function useDialogBehavior({
    aberto,
    onFechar,
    painelRef,
    focoInicialRef,
    fecharAcimaDe,
}: UseDialogBehaviorOptions) {
    // Quase todo call site passa `onFechar={() => setX(false)}`, uma função nova
    // a cada render. Se ela entrasse nas dependências do efeito, o efeito
    // remontaria a cada tecla digitada e roubaria o foco do campo em uso.
    const onFecharRef = useRef(onFechar);
    onFecharRef.current = onFechar;

    useEffect(() => {
        if (!aberto) return;

        const id = Symbol('dialogo');
        pilha.push(id);

        const painel = painelRef.current;
        const focoAnterior = document.activeElement as HTMLElement | null;

        (focoInicialRef?.current ?? painel)?.focus();
        travarBody();

        const noTopo = () => pilha[pilha.length - 1] === id;

        const onKeyDown = (evento: KeyboardEvent) => {
            if (!noTopo()) return;

            if (evento.key === 'Escape') {
                evento.preventDefault();
                onFecharRef.current();
                return;
            }

            if (evento.key !== 'Tab' || !painel) return;

            const focaveis = Array.from(
                painel.querySelectorAll<HTMLElement>(SELETOR_FOCAVEL),
            ).filter((elemento) => elemento.offsetParent !== null);

            if (focaveis.length === 0) {
                evento.preventDefault();
                painel.focus();
                return;
            }

            const primeiro = focaveis[0];
            const ultimo = focaveis[focaveis.length - 1];
            const atual = document.activeElement;

            if (evento.shiftKey && (atual === primeiro || atual === painel)) {
                evento.preventDefault();
                ultimo.focus();
            } else if (!evento.shiftKey && atual === ultimo) {
                evento.preventDefault();
                primeiro.focus();
            }
        };

        // `capture: true` para receber a tecla antes de qualquer campo que
        // trate Escape por conta própria (selects nativos, por exemplo).
        document.addEventListener('keydown', onKeyDown, true);

        const mq = fecharAcimaDe
            ? window.matchMedia(`(min-width: ${fecharAcimaDe}px)`)
            : null;
        const onMudancaLargura = (evento: MediaQueryListEvent) => {
            if (evento.matches) onFecharRef.current();
        };
        mq?.addEventListener('change', onMudancaLargura);

        // O listener acima só cobre a *travessia* do breakpoint. Se o diálogo
        // abrir já acima dele — estado restaurado, abertura por código — nenhum
        // evento chega e ele ficaria preso aberto, com a rolagem do fundo
        // travada. Conferir o valor atual fecha essa brecha sem depender de
        // evento nenhum.
        if (mq?.matches) {
            onFecharRef.current();
        }

        return () => {
            document.removeEventListener('keydown', onKeyDown, true);
            mq?.removeEventListener('change', onMudancaLargura);
            destravarBody();

            const indice = pilha.indexOf(id);
            if (indice !== -1) pilha.splice(indice, 1);

            focoAnterior?.focus?.();
        };
    }, [aberto, fecharAcimaDe, painelRef, focoInicialRef]);
}
