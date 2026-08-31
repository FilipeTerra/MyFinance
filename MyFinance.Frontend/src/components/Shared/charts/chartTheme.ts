import { formatCurrency } from '../../Calculadora/calculadoraUtils';

/**
 * Fonte única de verdade para cores e configuração de gráficos (recharts recebe props, não CSS,
 * então os mesmos hex são espelhados em src/index.css como `--chart-*` para os elementos de
 * legenda/pontinho renderizados em HTML — mantenha os dois lados sincronizados).
 */

export const CHART_MARGIN = { top: 8, right: 16, left: 8, bottom: 0 } as const;

export const gridProps = { stroke: '#e2e8f0', vertical: false } as const;

export const xAxisProps = { stroke: '#94a3b8', fontSize: 12, tick: { fill: '#64748b' } } as const;

export const yAxisProps = { ...xAxisProps, width: 90 } as const;

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
