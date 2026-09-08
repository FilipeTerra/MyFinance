import { useCallback, useState } from 'react';
import type { VarianteFeedback } from '../components/Shared/ui/FeedbackModal';

export interface EstadoFeedback {
    variante: VarianteFeedback;
    titulo: string;
    mensagem: string;
    detalhes?: string[];
    /** Executado depois que o usuário dispensa o popup. */
    aoFechar?: () => void;
}

/**
 * Estado do popup de resultado de uma ação.
 *
 * Existe para as telas não repetirem quatro `useState` cada uma, e para o
 * `aoFechar` ficar amarrado ao próprio feedback: o que acontece depois de um
 * salvamento bem-sucedido (fechar o modal, recarregar a lista) precisa esperar
 * o usuário ler a confirmação, não disparar junto com ela.
 */
export function useFeedback() {
    const [feedback, setFeedback] = useState<EstadoFeedback | null>(null);

    const mostrarSucesso = useCallback(
        (mensagem: string, opcoes?: { titulo?: string; aoFechar?: () => void }) =>
            setFeedback({
                variante: 'sucesso',
                titulo: opcoes?.titulo ?? 'Tudo certo',
                mensagem,
                aoFechar: opcoes?.aoFechar,
            }),
        [],
    );

    const mostrarErro = useCallback(
        (mensagem: string, opcoes?: { titulo?: string; detalhes?: string[]; aoFechar?: () => void }) =>
            setFeedback({
                variante: 'erro',
                titulo: opcoes?.titulo ?? 'Não deu certo',
                mensagem,
                detalhes: opcoes?.detalhes,
                aoFechar: opcoes?.aoFechar,
            }),
        [],
    );

    const fechar = useCallback(() => {
        setFeedback(atual => {
            atual?.aoFechar?.();
            return null;
        });
    }, []);

    return { feedback, mostrarSucesso, mostrarErro, fechar };
}
