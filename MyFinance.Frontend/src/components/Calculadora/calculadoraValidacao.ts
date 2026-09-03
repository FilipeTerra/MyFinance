import { FonteTaxaJuros } from '../../types/FonteTaxaJuros';
import { parsePercent } from './calculadoraUtils';
import type { PrazoValue, TaxaRendimentoValue } from './calculadoraTypes';

/** Converte um PrazoValue (anos ou meses) no número de meses inteiro — expressão que estava repetida em 4 componentes. */
export function prazoParaMeses(prazo: PrazoValue): number {
    const numero = parseFloat(prazo.valor || '0');
    return Math.round(prazo.unidade === 'anos' ? numero * 12 : numero);
}

export function validarPrazo(prazo: PrazoValue): string | null {
    return prazoParaMeses(prazo) > 0 ? null : 'Informe um prazo válido maior que zero.';
}

/** Valida o campo de taxa manual/% CDI relevante ao modo escolhido — par de checagem que estava repetido em 4 componentes. */
export function validarTaxaRendimento(taxa: TaxaRendimentoValue): string | null {
    if (taxa.modo === 'manual') {
        const numero = parsePercent(taxa.taxaManual);
        if (numero === null || numero < 0) return 'Informe uma taxa de juros anual válida.';
    }
    if (taxa.modo === 'cdi') {
        const numero = parsePercent(taxa.percentualCdi);
        if (numero === null || numero < 0) return 'Informe um percentual do CDI válido.';
    }
    return null;
}

/** Mapeia o modo de taxa da UI para o enum que a API espera — ternário repetido em 5 lugares. */
export function fonteTaxaJurosDe(taxa: TaxaRendimentoValue): FonteTaxaJuros {
    if (taxa.modo === 'selic') return FonteTaxaJuros.Selic;
    if (taxa.modo === 'cdi') return FonteTaxaJuros.PercentualCdi;
    return FonteTaxaJuros.Manual;
}

/** Monta os campos de taxa do payload da API a partir do estado da UI. */
export function parametrosTaxa(taxa: TaxaRendimentoValue): {
    fonteTaxaJuros: FonteTaxaJuros;
    taxaJurosAnualPercentual?: number;
    percentualCdi?: number;
} {
    return {
        fonteTaxaJuros: fonteTaxaJurosDe(taxa),
        taxaJurosAnualPercentual: taxa.modo === 'manual' ? (parsePercent(taxa.taxaManual) ?? undefined) : undefined,
        percentualCdi: taxa.modo === 'cdi' ? (parsePercent(taxa.percentualCdi) ?? undefined) : undefined,
    };
}

/** "18" → "1 ano e 6 meses" — helper que estava definido duas vezes, byte-a-byte igual. */
export function formatPrazo(meses: number): string {
    const anos = Math.floor(meses / 12);
    const restoMeses = meses % 12;
    if (anos === 0) return `${meses} ${meses === 1 ? 'mês' : 'meses'}`;
    if (restoMeses === 0) return `${anos} ${anos === 1 ? 'ano' : 'anos'}`;
    return `${anos} ${anos === 1 ? 'ano' : 'anos'} e ${restoMeses} ${restoMeses === 1 ? 'mês' : 'meses'}`;
}
