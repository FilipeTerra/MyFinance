import type { ReactNode } from 'react';
import './Alerta.css';

interface AlertaProps {
    variante?: 'erro' | 'aviso' | 'sucesso';
    children: ReactNode;
    /** Rótulo do botão de ação (ex.: "Tentar novamente"). Omitido = sem botão. */
    rotuloAcao?: string;
    onAcao?: () => void;
}

/**
 * Faixa de alerta com semântica de acessibilidade correta
 * (`role="alert"`/`aria-live`), algo que não existia em nenhum dos avisos
 * do app até aqui. `erro` é sempre anunciado com urgência (`assertive`);
 * `aviso`/`sucesso` são anunciados sem interromper o usuário (`polite`).
 */
export function Alerta({ variante = 'erro', children, rotuloAcao, onAcao }: AlertaProps) {
    return (
        <div
            className={`alerta alerta--${variante}`}
            role="alert"
            aria-live={variante === 'erro' ? 'assertive' : 'polite'}
        >
            <span className="alerta-mensagem">{children}</span>
            {rotuloAcao && onAcao && (
                <button type="button" className="alerta-btn-acao" onClick={onAcao}>
                    {rotuloAcao}
                </button>
            )}
        </div>
    );
}
