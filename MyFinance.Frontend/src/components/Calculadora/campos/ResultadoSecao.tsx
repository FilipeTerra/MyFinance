import type { ReactNode, RefObject } from 'react';
import './campos.css';

interface ResultadoSecaoProps {
    resultadoRef: RefObject<HTMLDivElement | null>;
    titulo?: string;
    /** Eco dos parâmetros usados no cálculo (ex.: "R$ 500/mês por 10 anos em Tesouro Selic"). */
    eco?: ReactNode;
    children: ReactNode;
}

/**
 * Envelope visual do resultado — superfície com destaque na borda superior,
 * distinta do formulário (antes os dois usavam a mesma receita de card,
 * separados só por um espaço). `tabIndex={-1}` permite receber foco
 * programático via `useResultadoFoco` sem entrar na ordem de tab normal.
 */
export function ResultadoSecao({ resultadoRef, titulo = 'Resultado da simulação', eco, children }: ResultadoSecaoProps) {
    return (
        <section ref={resultadoRef} className="calc-resultado-secao" tabIndex={-1} aria-live="polite">
            <h3 className="calc-resultado-titulo">{titulo}</h3>
            {eco && <p className="calc-resultado-eco">{eco}</p>}
            {children}
        </section>
    );
}
