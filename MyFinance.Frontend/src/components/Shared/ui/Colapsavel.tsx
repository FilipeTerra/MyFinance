import type { ReactNode } from 'react';
import './Colapsavel.css';

interface ColapsavelProps {
    titulo: string;
    /** Selo já formatado ao lado do título (ex.: "2 ativas", "7 categorias") — impede que o usuário esconda estado configurado sem perceber. */
    selo?: string;
    children: ReactNode;
    defaultAberto?: boolean;
}

/**
 * `<details>`/`<summary>` nativo: semântica e teclado de graça, sem
 * `useState`. Usado nas "Opções avançadas" da Calculadora e na cauda do
 * ranking de categorias em Gastos.
 */
export function Colapsavel({ titulo, selo, children, defaultAberto = false }: ColapsavelProps) {
    return (
        <details className="colapsavel" open={defaultAberto}>
            <summary className="colapsavel-titulo">
                {titulo}
                {selo && <span className="colapsavel-selo">{selo}</span>}
            </summary>
            <div className="colapsavel-conteudo">{children}</div>
        </details>
    );
}
