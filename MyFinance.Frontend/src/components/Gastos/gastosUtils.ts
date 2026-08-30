import type { CategoryExpenseDto } from '../../types/ExpenseAnalytics';
import { formatCurrency } from '../Calculadora/calculadoraUtils';

export { formatCurrency };

/** Categorias mostradas individualmente antes de agrupar o restante em "Outras" (limite de 6 fatias no donut). */
export const MAX_CATEGORIAS_INDIVIDUAIS = 5;

/**
 * Paleta categórica de identidade — mesma ordem validada (fixa, nunca reciclada) já usada em
 * `ComparadorCenarios` (CORES_CENARIO), estendida até o 6º tom. Ver skill dataviz / palette.md.
 */
export const CORES_CATEGORIA = ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4', '#008300'];

/** Cor neutra para a fatia "Outras" — não faz parte da identidade categórica. */
export const COR_OUTRAS = '#94a3b8';

/**
 * Mapa categoria → cor, construído a partir de uma lista já ordenada da maior para a menor
 * despesa (tipicamente `overview.categories`). Mantém a mesma cor para a mesma categoria em
 * todos os gráficos da seção, mesmo quando um gráfico agrupa o restante em "Outras".
 */
export function construirMapaCoresCategorias(categoriasOrdenadas: { categoryId: string }[]): Map<string, string> {
    const mapa = new Map<string, string>();
    categoriasOrdenadas.forEach((categoria, index) => {
        mapa.set(categoria.categoryId, index < CORES_CATEGORIA.length ? CORES_CATEGORIA[index] : COR_OUTRAS);
    });
    return mapa;
}

export function corDaCategoria(mapa: Map<string, string>, categoryId: string): string {
    return mapa.get(categoryId) ?? COR_OUTRAS;
}

export interface CategoriaAgrupada extends CategoryExpenseDto {
    isOutras?: boolean;
}

/** Reduz uma lista de categorias às `max` maiores, somando o restante em uma linha "Outras". */
export function agruparTopCategorias(categorias: CategoryExpenseDto[], max = MAX_CATEGORIAS_INDIVIDUAIS): CategoriaAgrupada[] {
    if (categorias.length <= max) return categorias;
    const principais = categorias.slice(0, max);
    const restante = categorias.slice(max);
    const outras: CategoriaAgrupada = {
        categoryId: '__outras__',
        categoryName: 'Outras',
        total: restante.reduce((soma, c) => soma + c.total, 0),
        percentage: restante.reduce((soma, c) => soma + c.percentage, 0),
        transactionCount: restante.reduce((soma, c) => soma + c.transactionCount, 0),
        isOutras: true,
    };
    return [...principais, outras];
}

/** "2026-08" → "ago/26" (rótulo curto para eixo de gráfico). */
export function formatMesLabel(label: string): string {
    const [ano, mes] = label.split('-').map(Number);
    const data = new Date(Date.UTC(ano, mes - 1, 1));
    return new Intl.DateTimeFormat('pt-BR', { month: 'short', year: '2-digit', timeZone: 'UTC' })
        .format(data)
        .replace('.', '');
}

/** "2026-08" → "agosto de 2026" (rótulo completo para tooltip). */
export function formatMesLabelCompleto(label: string): string {
    const [ano, mes] = label.split('-').map(Number);
    const data = new Date(Date.UTC(ano, mes - 1, 1));
    return new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(data);
}

export function formatSignedPercent(value: number | null): string {
    if (value === null) return '—';
    return `${value > 0 ? '+' : ''}${value.toFixed(1)}%`;
}

export function formatSignedCurrency(value: number): string {
    const sinal = value > 0 ? '+' : value < 0 ? '-' : '';
    return `${sinal}${formatCurrency(Math.abs(value))}`;
}
