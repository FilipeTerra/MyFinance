import type { ReactNode } from 'react';
import { Modal } from './Modal';
import './FeedbackModal.css';

export type VarianteFeedback = 'sucesso' | 'erro';

export interface FeedbackModalProps {
    variante: VarianteFeedback;
    titulo: string;
    mensagem: string;
    /** Uma linha por problema. Usado quando o servidor detalha o que corrigir. */
    detalhes?: string[];
    onFechar: () => void;
    /** Padrão "Entendi". */
    rotuloFechar?: string;
}

const ICONES: Record<VarianteFeedback, ReactNode> = {
    sucesso: (
        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
    ),
    erro: (
        <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m0 3.75h.008v.008H12v-.008zM21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
    ),
};

/**
 * Diálogo de resultado de uma ação — o "deu certo"/"deu erro" padrão do app,
 * no lugar do `alert()` do navegador, que bloqueia a página, ignora o visual do
 * sistema e não comporta uma lista de erros.
 *
 * Monta sobre o `Modal` compartilhado, então herda foco preso, Escape, rolagem
 * travada e devolução de foco ao fechar.
 */
export function FeedbackModal({
    variante,
    titulo,
    mensagem,
    detalhes,
    onFechar,
    rotuloFechar = 'Entendi',
}: FeedbackModalProps) {
    return (
        <Modal
            onFechar={onFechar}
            titulo={titulo}
            tamanho="sm"
            semCabecalho
            className={`feedback-modal feedback-modal--${variante}`}
            rodape={
                <button type="button" className="feedback-modal-btn" onClick={onFechar} autoFocus>
                    {rotuloFechar}
                </button>
            }
        >
            {/* `role`/`aria-live` para o resultado ser anunciado, e não só visto. */}
            <div
                className="feedback-modal-body"
                role={variante === 'erro' ? 'alert' : 'status'}
                aria-live={variante === 'erro' ? 'assertive' : 'polite'}
            >
                <div className="feedback-modal-icone">
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                        {ICONES[variante]}
                    </svg>
                </div>

                <h3 className="feedback-modal-titulo">{titulo}</h3>
                <p className="feedback-modal-mensagem">{mensagem}</p>

                {detalhes && detalhes.length > 0 && (
                    <ul className="feedback-modal-detalhes">
                        {detalhes.map((detalhe, i) => (
                            <li key={i}>{detalhe}</li>
                        ))}
                    </ul>
                )}
            </div>
        </Modal>
    );
}
