import { useState } from 'react';
import type { ExpenseOverviewResponseDto, ExpenseTimelineResponseDto } from '../../types/ExpenseAnalytics';
import type { RankingCategorias } from './gastosSelectors';
import { Card, SegmentedControl } from '../Shared/ui';
import { GastosFluxoMensal } from './GastosFluxoMensal';
import { GastosEvolucaoTemporal } from './GastosEvolucaoTemporal';
import { GastosComparacaoPeriodos } from './GastosComparacaoPeriodos';
import './GastosEvolucao.css';

type Visao = 'fluxo' | 'composicao' | 'tendencia' | 'comparacao';

const OPCOES: { valor: Visao; rotulo: string }[] = [
    { valor: 'fluxo', rotulo: 'Fluxo mensal' },
    { valor: 'composicao', rotulo: 'Composição' },
    { valor: 'tendencia', rotulo: 'Tendência' },
    { valor: 'comparacao', rotulo: 'vs. anterior' },
];

interface GastosEvolucaoProps {
    overview: ExpenseOverviewResponseDto;
    timeline: ExpenseTimelineResponseDto;
    ranking: RankingCategorias;
}

/**
 * Antes os 4 gráficos desta seção (fluxo, composição, tendência, comparação)
 * ficavam todos visíveis ao mesmo tempo, empilhados — a maior fonte da
 * "parede" de ~2200px da aba. Agora só um é exibido por vez, com largura
 * total, escolhido por este seletor.
 */
export function GastosEvolucao({ overview, timeline, ranking }: GastosEvolucaoProps) {
    const [visao, setVisao] = useState<Visao>('fluxo');

    return (
        <Card>
            <div className="evolucao-header">
                <h3 className="gastos-card-title">Evolução no período</h3>
                <SegmentedControl
                    value={visao}
                    onChange={setVisao}
                    ariaLabel="Visão da evolução"
                    variante="pill"
                    tamanho="sm"
                    rolavel
                    opcoes={OPCOES}
                />
            </div>

            {visao === 'fluxo' && <GastosFluxoMensal timeline={timeline} />}
            {visao === 'composicao' && <GastosEvolucaoTemporal timeline={timeline} ranking={ranking} visao="composicao" />}
            {visao === 'tendencia' && <GastosEvolucaoTemporal timeline={timeline} ranking={ranking} visao="tendencia" />}
            {visao === 'comparacao' && <GastosComparacaoPeriodos overview={overview} />}
        </Card>
    );
}
