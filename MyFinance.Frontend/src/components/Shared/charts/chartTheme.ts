import { formatCurrency } from '../../Calculadora/calculadoraUtils';

/**
 * Fonte única de verdade para cores e configuração de gráficos (recharts recebe props, não CSS,
 * então os mesmos hex são espelhados em src/index.css como `--chart-*` para os elementos de
 * legenda/pontinho renderizados em HTML — mantenha os dois lados sincronizados).
 */

export const CHART_MARGIN = { top: 8, right: 16, left: 8, bottom: 0 } as const;

export const gridProps = { stroke: '#e2e8f0', vertical: false } as const;

export const xAxisProps = { stroke: '#94a3b8', fontSize: 12, tick: { fill: '#64748b' } } as const;

/**
 * `width: 90` num eixo Y consome 24% de um viewport de 375px antes de existir
 * área de plotagem — daí a versão compacta (44px), a única prop de gráfico que
 * varia por largura de tela; ver `useIsMobile`. Nenhum gráfico do app ainda
 * espalhava este objeto (cada um reescrevia `stroke`/`fontSize`/`tick` à mão
 * inline) — vira função aqui, sem quebrar nada existente, para que os 8 pontos
 * de uso possam consumi-la de uma vez.
 */
export const yAxisProps = (compacto: boolean) => ({
    ...xAxisProps,
    width: compacto ? 44 : 90,
});

export const BAR_RADIUS: [number, number, number, number] = [4, 4, 0, 0];

export const MAX_BAR_SIZE = 20;

/** Intervalo de ticks do eixo X para não sobrepor rótulos em séries longas. */
export const intervaloEixoX = (quantidadePontos: number): number =>
    Math.max(0, Math.ceil(quantidadePontos / 10) - 1);

/** Formata o número do mês/parcela do eixo X como "Xm" (curto) ou "Xa" (anos, séries longas). */
export const tickPrazo = (totalPontos: number) => (mes: number): string =>
    totalPontos > 24 ? `${Math.round(mes / 12)}a` : `${mes}m`;

export const tooltipMoedaFormatter = (value: unknown, name: string): [string, string] => [
    formatCurrency(Number(value)),
    name,
];

/**
 * Versão compacta de `formatCurrency` para o eixo Y no celular ("R$ 1,2 mil"
 * em vez de "R$ 1.200,00") — mesmo com `width: 44`, o valor completo não cabe
 * sem quebrar. Só para rótulo de eixo; o tooltip continua com o valor exato
 * via `tooltipMoedaFormatter`.
 */
export const formatCurrencyCompacta = (value: number): string => {
    const absoluto = Math.abs(value);
    const sinal = value < 0 ? '-' : '';

    const compactar = (divisor: number, sufixo: string): string => {
        const numero = absoluto / divisor;
        const texto = numero % 1 === 0 ? numero.toFixed(0) : numero.toFixed(1).replace('.', ',');
        return `${sinal}R$ ${texto} ${sufixo}`;
    };

    if (absoluto >= 1_000_000) return compactar(1_000_000, 'mi');
    if (absoluto >= 1_000) return compactar(1_000, 'mil');
    return formatCurrency(value);
};

/**
 * Paleta categórica de identidade — mesma ordem validada (fixa, nunca reciclada) usada por
 * `ComparadorCenarios` e `AnaliseGastos`. Ver skill dataviz / palette.md.
 */
export const CORES_CATEGORIA = ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4', '#008300'];

/** Cor neutra para agrupamentos "Outras"/"Outros" — não faz parte da identidade categórica. */
export const COR_NEUTRA = '#94a3b8';

/**
 * Cores semânticas por série. Note que a comparação por categoria usa "alta = vermelho, queda =
 * verde" (gasto subindo é ruim), o oposto de um gráfico de rendimento onde alta seria positiva —
 * por isso `COR_ALTA`/`COR_QUEDA` são nomeadas pelo sinal da variação, não por "bom"/"ruim".
 */
export const COR_RECEITA = '#3b82f6';
export const COR_DESPESA = '#ef4444';
export const COR_SALDO = '#64748b';
export const COR_ALTA = '#ef4444';
export const COR_QUEDA = '#10b981';
