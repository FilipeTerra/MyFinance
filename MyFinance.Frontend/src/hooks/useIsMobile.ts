import { useEffect, useState } from 'react';

/**
 * Único ponto de responsividade em JavaScript do app — todo o resto é CSS.
 *
 * Existe porque os gráficos Recharts recebem número, não `@media`: a largura
 * já se adapta via `<ResponsiveContainer width="100%">`, mas altura e a
 * largura do eixo Y são props numéricas fixas. `768` mesmo, sem o `.98` que
 * as media queries CSS usam — não há cascata de regras concorrentes aqui, só
 * um valor booleano.
 */
export function useIsMobile(): boolean {
    const consulta = '(max-width: 767.98px)';
    const [ehMobile, setEhMobile] = useState(
        () => typeof window !== 'undefined' && window.matchMedia(consulta).matches,
    );

    useEffect(() => {
        const mq = window.matchMedia(consulta);
        const onMudanca = (evento: MediaQueryListEvent) => setEhMobile(evento.matches);
        mq.addEventListener('change', onMudanca);
        setEhMobile(mq.matches);
        return () => mq.removeEventListener('change', onMudanca);
    }, []);

    return ehMobile;
}
