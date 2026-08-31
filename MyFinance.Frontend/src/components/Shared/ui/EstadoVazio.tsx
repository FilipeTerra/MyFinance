import type { ReactNode } from 'react';
import './EstadoVazio.css';

interface EstadoVazioProps {
    /** hero = herói tracejado de página inteira (nada de dados no período); inline = compacto dentro de um card, com altura mínima fixa para o card não mudar de tamanho. */
    variante?: 'hero' | 'inline';
    icone?: ReactNode;
    titulo: string;
    descricao?: ReactNode;
    acao?: ReactNode;
}

/**
 * Estado vazio único para o app — antes havia 4 tratamentos diferentes só
 * na aba Gastos (herói tracejado, parágrafo cinza com 3 textos diferentes,
 * e um card que simplesmente sumia sem aviso).
 */
export function EstadoVazio({ variante = 'inline', icone, titulo, descricao, acao }: EstadoVazioProps) {
    return (
        <div className={`estado-vazio estado-vazio--${variante}`}>
            {icone && <div className="estado-vazio-icone" aria-hidden="true">{icone}</div>}
            <h3 className="estado-vazio-titulo">{titulo}</h3>
            {descricao && <p className="estado-vazio-desc">{descricao}</p>}
            {acao && <div className="estado-vazio-acao">{acao}</div>}
        </div>
    );
}
