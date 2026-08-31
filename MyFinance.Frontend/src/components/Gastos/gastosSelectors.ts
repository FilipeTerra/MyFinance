import type { CategoryExpenseDto } from '../../types/ExpenseAnalytics';
import { CORES_CATEGORIA, COR_NEUTRA } from '../Shared/charts/chartTheme';

export type PeriodoPreset = '3m' | '6m' | '12m' | 'ano' | 'custom';

/** Categorias que ganham cor própria — as demais entram em "Outras". Um único corte para toda a aba (antes havia 3 cortes diferentes: 5 no donut, 5 na evolução — de outra fonte — e 8 na comparação). */
export const MAX_CATEGORIAS_IDENTIDADE = 5;

export interface RankingCategorias {
    /** Já vem ordenado do maior para o menor (garantia da API). */
    principais: CategoryExpenseDto[];
    outras: CategoryExpenseDto[];
    temOutras: boolean;
    totalOutras: number;
    transacoesOutras: number;
    /** Só as `principais` têm entrada aqui — categorias em "outras" não têm cor de identidade. */
    mapaCores: Map<string, string>;
}

/**
 * Constrói o ranking de categorias UMA vez, a partir de `overview.categories`
 * (já ordenado pela API) — usado por todo mundo na aba (donut, ranking,
 * evolução por composição/tendência), em vez de cada gráfico recalcular seu
 * próprio corte de "top N" a partir de uma fonte diferente.
 */
export function construirRanking(categorias: CategoryExpenseDto[]): RankingCategorias {
    const principais = categorias.slice(0, MAX_CATEGORIAS_IDENTIDADE);
    const outras = categorias.slice(MAX_CATEGORIAS_IDENTIDADE);
    const mapaCores = new Map<string, string>();
    principais.forEach((categoria, indice) => mapaCores.set(categoria.categoryId, CORES_CATEGORIA[indice] ?? COR_NEUTRA));

    return {
        principais,
        outras,
        temOutras: outras.length > 0,
        totalOutras: outras.reduce((soma, c) => soma + c.total, 0),
        transacoesOutras: outras.reduce((soma, c) => soma + c.transactionCount, 0),
        mapaCores,
    };
}

/** Cor de uma categoria — `COR_NEUTRA` para qualquer uma fora do top 5 (nunca reaproveita a cor de outra categoria). */
export function corDaCategoria(ranking: RankingCategorias, categoryId: string): string {
    return ranking.mapaCores.get(categoryId) ?? COR_NEUTRA;
}

const FORMATADOR_DATA_CURTA = new Intl.DateTimeFormat('pt-BR', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' });

const formatarDataCurta = (iso: string): string => FORMATADOR_DATA_CURTA.format(new Date(`${iso}T00:00:00Z`)).replace('.', '');

/** "2026-06-01".."2026-08-30" → "1 jun 2026 – 30 ago 2026". */
export function descreverIntervalo(startDate: string, endDate: string): string {
    return `${formatarDataCurta(startDate)} – ${formatarDataCurta(endDate)}`;
}

const ultimoDiaDoMes = (ano: number, mesIndiceZero: number): number => new Date(Date.UTC(ano, mesIndiceZero + 1, 0)).getUTCDate();

/**
 * Retorna o rótulo "yyyy-MM" do mês em curso quando `endDate` (sempre "hoje"
 * nos presets fixos) não fecha o mês inteiro — ou seja, quando o período
 * embute um mês parcial que distorce médias e encurta a última barra dos
 * gráficos. Para o preset "custom" o usuário escolheu a data de propósito,
 * então nunca é tratado como parcial.
 */
export function calcularMesParcial(preset: PeriodoPreset, endDate: string): string | null {
    if (preset === 'custom') return null;

    const fim = new Date(`${endDate}T00:00:00Z`);
    const ano = fim.getUTCFullYear();
    const mesIndiceZero = fim.getUTCMonth();
    if (fim.getUTCDate() === ultimoDiaDoMes(ano, mesIndiceZero)) return null;

    return `${ano}-${String(mesIndiceZero + 1).padStart(2, '0')}`;
}
