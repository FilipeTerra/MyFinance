export const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

/** Máscara monetária: converte dígitos digitados em "1.234,56". */
export function maskCurrency(raw: string): string {
    let digits = raw.replace(/\D/g, '');
    if (digits === '') return '';
    if (digits.length > 1) digits = digits.replace(/^0+/, '');
    while (digits.length < 3) digits = '0' + digits;
    const decimalIndex = digits.length - 2;
    const integerPart = digits.slice(0, decimalIndex);
    const decimalPart = digits.slice(decimalIndex);
    const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    return formattedInteger + ',' + decimalPart;
}

export const parseCurrency = (masked: string): number => {
    const parsed = parseFloat(masked.replace(/\./g, '').replace(',', '.'));
    return isNaN(parsed) ? 0 : parsed;
};

export const parsePercent = (raw: string): number | null => {
    const parsed = parseFloat(raw.replace(',', '.'));
    return isNaN(parsed) ? null : parsed;
};
