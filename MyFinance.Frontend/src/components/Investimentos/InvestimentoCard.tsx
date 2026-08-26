import { useState } from 'react';
import type { InvestimentoResponseDto } from '../../types/InvestimentoResponseDto';
import { investimentoService } from '../../services/Api';
import { INVESTMENT_TYPE_META } from './investmentTypeMeta';
import { InvestimentoModal } from './InvestimentoModal';
import { AporteInvestimentoModal } from './AporteInvestimentoModal';
import { HistoricoAportesModal } from './HistoricoAportesModal';
import { CotacaoSparkline } from './CotacaoSparkline';
import './InvestimentoCard.css';

interface InvestimentoCardProps {
    investimento: InvestimentoResponseDto;
    onUpdateSuccess: () => void;
    onDeleteSuccess: () => void;
}

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

const formatDate = (isoString: string) =>
    new Intl.DateTimeFormat('pt-BR').format(new Date(isoString));

type Trend = 'up' | 'down' | 'flat';

function getTrend(rentabilidade: number): Trend {
    if (rentabilidade > 0.001) return 'up';
    if (rentabilidade < -0.001) return 'down';
    return 'flat';
}

const TREND_META: Record<Trend, { arrow: string; label: string }> = {
    up:   { arrow: '▲', label: 'Valorização' },
    down: { arrow: '▼', label: 'Desvalorização' },
    flat: { arrow: '—', label: 'Estável' },
};

export function InvestimentoCard({ investimento, onUpdateSuccess, onDeleteSuccess }: InvestimentoCardProps) {
    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [isAporteModalOpen, setIsAporteModalOpen] = useState(false);
    const [isHistoricoModalOpen, setIsHistoricoModalOpen] = useState(false);
    const [isConfirmingDelete, setIsConfirmingDelete] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);

    const handleDelete = async () => {
        setIsDeleting(true);
        try {
            await investimentoService.delete(investimento.id);
            onDeleteSuccess();
        } catch {
            alert('Não foi possível excluir o investimento. Tente novamente.');
            setIsConfirmingDelete(false);
        } finally {
            setIsDeleting(false);
        }
    };

    const meta = INVESTMENT_TYPE_META[investimento.tipo];
    const trend = getTrend(investimento.rentabilidadePercentual);
    const trendMeta = TREND_META[trend];
    const resultado = investimento.valorAtual - investimento.totalAportado;

    return (
        <div
            className={`inv-card inv-card--${trend}`}
            style={{ '--type-color': meta.color } as React.CSSProperties}
        >
            <div className="inv-card-header">
                <div className="inv-card-heading">
                    <span className="inv-type-badge">
                        <span className="inv-type-badge-icon" aria-hidden="true">{meta.icon}</span>
                        {meta.label}
                    </span>
                    <h3 className="inv-card-name">{investimento.nome}</h3>
                </div>
            </div>

            <div className="inv-value-section">
                <div className="inv-value-columns">
                    <div className="inv-value-col">
                        <span className="inv-value-label">Total aportado</span>
                        <span className="inv-value-amount">{formatCurrency(investimento.totalAportado)}</span>
                    </div>
                    <div className="inv-value-col inv-value-col--current">
                        <span className="inv-value-label">Valor atual</span>
                        <span className="inv-value-amount inv-value-amount--current">{formatCurrency(investimento.valorAtual)}</span>
                    </div>
                </div>

                <div className={`inv-trend inv-trend--${trend}`}>
                    <span className="inv-trend-arrow" aria-hidden="true">{trendMeta.arrow}</span>
                    {investimento.rentabilidadePercentual > 0 ? '+' : ''}
                    {investimento.rentabilidadePercentual.toFixed(2)}%
                    <span className="inv-trend-abs">
                        ({resultado >= 0 ? '+' : ''}{formatCurrency(resultado)})
                    </span>
                </div>

                {investimento.ticker && (
                    <span className="inv-auto-sync-badge" title="A cotação deste ativo é atualizada automaticamente pela integração com a bolsa">
                        🔄 Cotação automática ({investimento.ticker})
                    </span>
                )}
            </div>

            <div className="inv-variacao-block">
                <div className="inv-variacao-info">
                    <span className="inv-variacao-label">Últimos 3 meses</span>
                    {investimento.variacaoUltimos3MesesPercentual !== null ? (
                        <span className={`inv-variacao-value inv-variacao-value--${
                            investimento.variacaoUltimos3MesesPercentual > 0 ? 'up' :
                            investimento.variacaoUltimos3MesesPercentual < 0 ? 'down' : 'flat'
                        }`}>
                            {investimento.variacaoUltimos3MesesPercentual > 0 ? '+' : ''}
                            {investimento.variacaoUltimos3MesesPercentual.toFixed(2)}%
                        </span>
                    ) : (
                        <span className="inv-variacao-value inv-variacao-value--flat">Dados insuficientes</span>
                    )}
                </div>
                <CotacaoSparkline pontos={investimento.historicoCotacoes} />
            </div>

            <div className="inv-footer">
                <span className="inv-footer-label">Desde</span>
                <span className="inv-footer-value">{formatDate(investimento.dataCriacao)}</span>
            </div>

            {isConfirmingDelete ? (
                <div className="inv-delete-confirm">
                    <span className="inv-delete-confirm-text">Excluir este investimento?</span>
                    <button
                        className="inv-delete-confirm-no"
                        onClick={() => setIsConfirmingDelete(false)}
                        disabled={isDeleting}
                    >
                        Cancelar
                    </button>
                    <button
                        className="inv-delete-confirm-yes"
                        onClick={handleDelete}
                        disabled={isDeleting}
                    >
                        {isDeleting ? 'Excluindo...' : 'Confirmar'}
                    </button>
                </div>
            ) : (
                <>
                    <div className="inv-actions">
                        {!investimento.ticker && (
                            <button
                                className="inv-update-btn"
                                onClick={() => setIsEditModalOpen(true)}
                                title="Atualizar valor de mercado"
                            >
                                Atualizar valor
                            </button>
                        )}
                        <button
                            className="inv-aporte-btn"
                            onClick={() => setIsAporteModalOpen(true)}
                            title="Registrar um novo aporte"
                        >
                            Aportar mais
                        </button>
                        <button
                            className="inv-delete-btn"
                            onClick={() => setIsConfirmingDelete(true)}
                            title="Excluir este investimento"
                        >
                            Excluir
                        </button>
                    </div>
                    <button className="inv-historico-link" onClick={() => setIsHistoricoModalOpen(true)}>
                        Ver histórico de aportes
                    </button>
                </>
            )}

            {isEditModalOpen && (
                <InvestimentoModal
                    mode="edit"
                    investimento={investimento}
                    onClose={() => setIsEditModalOpen(false)}
                    onSuccess={() => {
                        setIsEditModalOpen(false);
                        onUpdateSuccess();
                    }}
                />
            )}

            {isAporteModalOpen && (
                <AporteInvestimentoModal
                    investimentoId={investimento.id}
                    investimentoNome={investimento.nome}
                    onClose={() => setIsAporteModalOpen(false)}
                    onSuccess={() => {
                        setIsAporteModalOpen(false);
                        onUpdateSuccess();
                    }}
                />
            )}

            {isHistoricoModalOpen && (
                <HistoricoAportesModal
                    investimentoId={investimento.id}
                    investimentoNome={investimento.nome}
                    onClose={() => setIsHistoricoModalOpen(false)}
                />
            )}
        </div>
    );
}
