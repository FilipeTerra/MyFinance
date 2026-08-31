import type { HTMLAttributes, ReactNode } from 'react';
import './Card.css';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
    /** padded = card padrão (1.5rem); compacto = card denso (tabelas/tributos); grafico = padding assimétrico para gráficos recharts. */
    variante?: 'padded' | 'compacto' | 'grafico';
    children: ReactNode;
}

/**
 * Superfície canônica reaproveitada em todo o app: branco, borda slate-200,
 * raio xl, sombra sm. Substitui a mesma receita que estava copiada em
 * `.proj-form`, `.proj-result-stats`, `.proj-tributos`, `.proj-chart`,
 * `.cmp-tabela-wrap`, `.fin-tabela-wrap`, `.gastos-card` e `.dashboard-summary`.
 */
export function Card({ variante = 'padded', className, children, ...rest }: CardProps) {
    const classes = ['card', `card--${variante}`, className].filter(Boolean).join(' ');
    return (
        <div className={classes} {...rest}>
            {children}
        </div>
    );
}
