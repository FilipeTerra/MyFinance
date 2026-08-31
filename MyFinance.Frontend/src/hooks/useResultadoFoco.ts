import { useEffect, useRef } from 'react';

/**
 * Rola até e foca a seção de resultado sempre que `gatilho` mudar (ex.: o
 * objeto retornado por um cálculo bem-sucedido). Sem isso, o usuário clica
 * num botão de submit a mais de 1000px de profundidade e não recebe nenhum
 * sinal de que um resultado apareceu mais abaixo — o maior problema de UX
 * da Calculadora antes deste ajuste.
 */
export function useResultadoFoco<T>(gatilho: T | null | undefined) {
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (!gatilho || !ref.current) return;
        const prefereReduzido = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        ref.current.scrollIntoView({ block: 'start', behavior: prefereReduzido ? 'auto' : 'smooth' });
        ref.current.focus({ preventScroll: true });
    }, [gatilho]);

    return ref;
}
