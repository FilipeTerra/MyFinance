import { useCallback, useRef, type KeyboardEvent } from 'react';

/**
 * Roving tabindex para um grupo `role="radiogroup"`/`role="tablist"`: um
 * único tab stop no item ativo, setas movem a seleção, Home/End vão para as
 * pontas. Sem isso, cada botão do grupo é um tab stop separado e as setas
 * não fazem nada — o padrão que os 4 grupos de toggle do app tinham antes.
 */
export function useRovingRadioGroup<T extends string | number>(
    valores: readonly T[],
    valorAtivo: T,
    onChange: (valor: T) => void,
    orientacao: 'horizontal' | 'vertical' = 'horizontal',
) {
    const containerRef = useRef<HTMLDivElement>(null);

    const focarIndice = useCallback((indice: number) => {
        const container = containerRef.current;
        if (!container) return;
        const itens = container.querySelectorAll<HTMLElement>('[role="radio"], [role="tab"]');
        itens[indice]?.focus();
    }, []);

    const handleKeyDown = useCallback((evento: KeyboardEvent) => {
        const indiceAtual = valores.indexOf(valorAtivo);
        if (indiceAtual === -1) return;

        const teclaProxima = orientacao === 'horizontal' ? 'ArrowRight' : 'ArrowDown';
        const teclaAnterior = orientacao === 'horizontal' ? 'ArrowLeft' : 'ArrowUp';

        let novoIndice: number | null = null;
        if (evento.key === teclaProxima) novoIndice = (indiceAtual + 1) % valores.length;
        else if (evento.key === teclaAnterior) novoIndice = (indiceAtual - 1 + valores.length) % valores.length;
        else if (evento.key === 'Home') novoIndice = 0;
        else if (evento.key === 'End') novoIndice = valores.length - 1;

        if (novoIndice !== null) {
            evento.preventDefault();
            onChange(valores[novoIndice]);
            focarIndice(novoIndice);
        }
    }, [valores, valorAtivo, onChange, orientacao, focarIndice]);

    return { containerRef, handleKeyDown };
}
