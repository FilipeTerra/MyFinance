/**
 * Tipos compartilhados pelos modos da Calculadora. Declarados uma única vez
 * aqui — antes eram redeclarados byte-a-byte em 4 arquivos.
 *
 * ⚠ ARMADILHA: `CalculadoraFinanciamento.tsx` tem seu próprio `PeriodicidadeTaxa`
 * ('mensal'|'anual'), que é um conceito DIFERENTE de `TaxaRendimentoModo`
 * ('selic'|'cdi'|'manual') — a periodicidade de uma taxa de empréstimo não
 * tem nada a ver com a fonte de uma taxa de rendimento. Os dois nunca devem
 * ser unificados nem renomeados para o mesmo nome genérico "TaxaModo".
 */

export type PrazoUnidade = 'anos' | 'meses';

export interface PrazoValue {
    valor: string;
    unidade: PrazoUnidade;
}

export type TaxaRendimentoModo = 'selic' | 'cdi' | 'manual';

export interface TaxaRendimentoValue {
    modo: TaxaRendimentoModo;
    taxaManual: string;
    percentualCdi: string;
}

export type ModoCalculadora = 'unico' | 'comparar' | 'meta-reversa' | 'retirada' | 'financiamento';

export type ReajusteModoUi = 'nenhum' | 'fixo' | 'ipca';

/**
 * Aporte inicial/mensal e prazo — os únicos três campos com significado
 * IDÊNTICO em "Cenário único" e "Comparar cenários" (os dois pedem
 * literalmente a mesma coisa). Compartilhado entre os dois para o usuário
 * não redigitar nada ao trocar de modo. Meta reversa e Retirada têm campos
 * com semântica própria (valor-alvo, saldo inicial…) e não entram aqui.
 */
export interface BaseAportePrazo {
    aporteInicial: string;
    aporteMensal: string;
    prazo: PrazoValue;
}

export const BASE_APORTE_PRAZO_INICIAL: BaseAportePrazo = {
    aporteInicial: '',
    aporteMensal: '',
    prazo: { valor: '10', unidade: 'anos' },
};
