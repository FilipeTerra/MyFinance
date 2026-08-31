import { Skeleton } from '../Shared/ui';
import './GastosSkeleton.css';

/**
 * Espelha a altura real do layout de Gastos (KPI + 3 cards) em vez de
 * reaproveitar o skeleton de cards da aba Metas — que tinha um raio de
 * borda diferente e nenhuma relação com esta estrutura, causando um salto
 * de layout perceptível no primeiro carregamento.
 */
export function GastosSkeleton() {
    return (
        <div className="gastos-skeleton">
            <Skeleton altura={72} />
            <Skeleton altura={360} />
            <Skeleton altura={420} />
            <Skeleton altura={260} />
        </div>
    );
}
