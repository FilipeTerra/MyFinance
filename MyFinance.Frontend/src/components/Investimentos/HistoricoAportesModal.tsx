import { useEffect, useState } from 'react';
import { Modal } from '../Shared/ui/Modal';
import { investimentoService } from '../../services/Api';
import type { AporteHistoricoResponseDto } from '../../types/InvestimentoResponseDto';
import './InvestimentoModal.css';
import './HistoricoAportesModal.css';

interface HistoricoAportesModalProps {
    investimentoId: string;
    investimentoNome: string;
    onClose: () => void;
}

const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

const formatDate = (isoString: string) =>
    new Intl.DateTimeFormat('pt-BR').format(new Date(isoString));

export function HistoricoAportesModal({ investimentoId, investimentoNome, onClose }: HistoricoAportesModalProps) {
    const [aportes, setAportes] = useState<AporteHistoricoResponseDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const load = async () => {
            try {
                setAportes(await investimentoService.getHistoricoAportes(investimentoId));
            } catch {
                setError('Não foi possível carregar o histórico de aportes.');
            } finally {
                setIsLoading(false);
            }
        };
        void load();
    }, [investimentoId]);

    return (
        <Modal
            onFechar={onClose}
            titulo="Histórico de Aportes"
            tamanho="md"
        >

                <div className="inv-edit-context">
                    <span className="inv-edit-context-name">{investimentoNome}</span>
                </div>

                {isLoading ? (
                    <p style={{ color: '#64748b', margin: '0.5rem 0' }}>Carregando...</p>
                ) : error ? (
                    <span className="inv-error">{error}</span>
                ) : aportes.length === 0 ? (
                    <p className="historico-empty">Nenhum aporte registrado ainda.</p>
                ) : (
                    <ul className="historico-list">
                        {aportes.map(aporte => (
                            <li key={aporte.transactionId} className="historico-item">
                                <div className="historico-item-info">
                                    <span className="historico-item-valor">{formatCurrency(aporte.valor)}</span>
                                    {aporte.contaNome && (
                                        <span className="historico-item-conta">{aporte.contaNome}</span>
                                    )}
                                </div>
                                <span className="historico-item-data">{formatDate(aporte.data)}</span>
                            </li>
                        ))}
                    </ul>
                )}
        </Modal>
    );
}
