import { useCallback, useState } from 'react';

/**
 * Estado de erro por campo + um slot separado para erro geral/servidor.
 * Antes cada formulário da Calculadora tinha uma única string de erro no
 * rodapé: só a primeira regra que falhava era reportada, o erro só limpava
 * no próximo submit, e um erro de rede ficava indistinguível de um erro de
 * digitação. Este hook resolve os três problemas de uma vez.
 */
export function useErrosFormulario<K extends string>() {
    const [erros, setErros] = useState<Partial<Record<K, string>>>({});
    const [erroGeral, setErroGeral] = useState<string | null>(null);

    const limpar = useCallback((campo: K) => {
        setErros(prev => {
            if (!(campo in prev)) return prev;
            const proximo = { ...prev };
            delete proximo[campo];
            return proximo;
        });
    }, []);

    const limparTudo = useCallback(() => {
        setErros({});
        setErroGeral(null);
    }, []);

    /**
     * Define todos os erros de uma vez e foca o campo do primeiro deles pelo
     * id do DOM. Recebe o mapa de erros recém-calculado (não lê `erros` do
     * estado, que ainda não teria sido atualizado neste ponto do handler).
     */
    const definirEFocar = useCallback((novos: Partial<Record<K, string>>, idsPorCampo: Partial<Record<K, string>>) => {
        setErros(novos);
        const primeiroCampo = Object.keys(novos)[0] as K | undefined;
        const id = primeiroCampo ? idsPorCampo[primeiroCampo] : undefined;
        if (id) document.getElementById(id)?.focus();
    }, []);

    return { erros, erroGeral, limpar, limparTudo, setErroGeral, definirEFocar };
}
