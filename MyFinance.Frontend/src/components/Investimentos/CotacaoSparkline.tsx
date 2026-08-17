import type { CotacaoPontoDto } from '../../types/InvestimentoResponseDto';

interface CotacaoSparklineProps {
    pontos: CotacaoPontoDto[];
}

const WIDTH = 100;
const HEIGHT = 28;
const PADDING = 2;

const formatDate = (isoString: string) =>
    new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit' }).format(new Date(isoString));

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

export function CotacaoSparkline({ pontos }: CotacaoSparklineProps) {
    if (pontos.length < 2) return null;

    const valores = pontos.map(p => p.valor);
    const min = Math.min(...valores);
    const max = Math.max(...valores);
    const range = max - min || 1;

    const coords = pontos.map((p, i) => {
        const x = PADDING + (i / (pontos.length - 1)) * (WIDTH - 2 * PADDING);
        const y = HEIGHT - PADDING - ((p.valor - min) / range) * (HEIGHT - 2 * PADDING);
        return `${x.toFixed(2)},${y.toFixed(2)}`;
    });

    const primeiro = pontos[0];
    const ultimo = pontos[pontos.length - 1];

    return (
        <svg
            className="inv-sparkline"
            viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
            preserveAspectRatio="none"
            role="img"
            aria-label={`Cotação de ${formatCurrency(primeiro.valor)} em ${formatDate(primeiro.data)} até ${formatCurrency(ultimo.valor)} em ${formatDate(ultimo.data)}`}
        >
            <title>
                {`${formatDate(primeiro.data)}: ${formatCurrency(primeiro.valor)} → ${formatDate(ultimo.data)}: ${formatCurrency(ultimo.valor)}`}
            </title>
            <polyline
                points={coords.join(' ')}
                fill="none"
                stroke="var(--t-color)"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
            />
        </svg>
    );
}
