import { formatCurrency } from '../Calculadora/calculadoraUtils';

export { formatCurrency };

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
