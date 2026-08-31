import './Skeleton.css';

interface SkeletonProps {
    altura: number | string;
    /** Largura opcional — por padrão ocupa 100% do container. */
    largura?: number | string;
    raio?: 'sm' | 'md' | 'lg' | 'xl';
    className?: string;
}

/** Bloco de shimmer genérico — usa a altura real do layout de destino em vez de um card genérico, para não causar salto de layout no primeiro paint. */
export function Skeleton({ altura, largura = '100%', raio = 'xl', className }: SkeletonProps) {
    return (
        <div
            className={`skeleton skeleton--${raio}${className ? ' ' + className : ''}`}
            style={{ height: altura, width: largura }}
            aria-hidden="true"
        />
    );
}
