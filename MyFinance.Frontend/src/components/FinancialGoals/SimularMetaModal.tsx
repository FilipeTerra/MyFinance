import { useState } from 'react';
import ReactDOM from 'react-dom';
import { metaReversaService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { SimularMetaResponseDto } from '../../types/MetaReversa';
import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';
import { FonteTaxaJuros } from '../../types/FonteTaxaJuros';
import { maskCurrency, parseCurrency, parsePercent, formatCurrency } from '../Calculadora/calculadoraUtils';
import { CampoTipoAtivo } from '../Calculadora/campos/CampoTipoAtivo';
import '../Calculadora/CalculadoraProjecao.css';
import './ContributeToGoalModal.css';

interface SimularMetaModalProps {
    goalId: string;
    goalName: string;
    onClose: () => void;
}

type TaxaModo = 'selic' | 'cdi' | 'manual';
type ModoAporte = 'verificar' | 'calcular';

export function SimularMetaModal({ goalId, goalName, onClose }: SimularMetaModalProps) {
    const [modoAporte, setModoAporte] = useState<ModoAporte>('calcular');
    const [aporteMensal, setAporteMensal] = useState('');
    const [taxaModo, setTaxaModo] = useState<TaxaModo>('selic');
    const [taxaManual, setTaxaManual] = useState('');
    const [percentualCdi, setPercentualCdi] = useState('100');
    const [tipoAtivo, setTipoAtivo] = useState<TipoAtivoCalculadora>(TipoAtivoCalculadora.TesouroSelic);

    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [resultado, setResultado] = useState<SimularMetaResponseDto | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setResultado(null);

        const aporteMensalNumero = modoAporte === 'verificar' ? parseCurrency(aporteMensal) : null;
        if (modoAporte === 'verificar' && (!aporteMensalNumero || aporteMensalNumero <= 0)) {
            setError('Informe um aporte mensal válido maior que zero.');
            return;
        }

        const taxaManualNumero = taxaModo === 'manual' ? parsePercent(taxaManual) : null;
        if (taxaModo === 'manual' && (taxaManualNumero === null || taxaManualNumero < 0)) {
            setError('Informe uma taxa de juros anual válida.');
            return;
        }

        const percentualCdiNumero = taxaModo === 'cdi' ? parsePercent(percentualCdi) : null;
        if (taxaModo === 'cdi' && (percentualCdiNumero === null || percentualCdiNumero < 0)) {
            setError('Informe um percentual do CDI válido.');
            return;
        }

        const fonteTaxaJuros = taxaModo === 'selic'
            ? FonteTaxaJuros.Selic
            : taxaModo === 'cdi'
                ? FonteTaxaJuros.PercentualCdi
                : FonteTaxaJuros.Manual;

        setIsLoading(true);
        try {
            const data = await metaReversaService.simularMeta(goalId, {
                aporteMensal: modoAporte === 'verificar' ? aporteMensalNumero! : undefined,
                fonteTaxaJuros,
                taxaJurosAnualPercentual: taxaModo === 'manual' ? taxaManualNumero! : undefined,
                percentualCdi: taxaModo === 'cdi' ? percentualCdiNumero! : undefined,
                tipoAtivo,
            });
            setResultado(data);
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Não foi possível simular a meta. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    return ReactDOM.createPortal(
        <div className="contribute-overlay" onClick={onClose}>
            <div className="contribute-modal" style={{ maxWidth: 520 }} onClick={e => e.stopPropagation()}>
                <div className="contribute-modal-header">
                    <h2 className="contribute-modal-title">Simular investimento — {goalName}</h2>
                    <button className="contribute-modal-close" onClick={onClose} aria-label="Fechar">×</button>
                </div>

                <form onSubmit={handleSubmit}>
                    <div className="proj-form-group">
                        <label>O que você quer descobrir?</label>
                        <div className="proj-toggle-group proj-toggle-group--full" role="radiogroup" aria-label="Modo de simulação">
                            <button
                                type="button"
                                role="radio"
                                aria-checked={modoAporte === 'calcular'}
                                className={`proj-toggle-btn${modoAporte === 'calcular' ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => setModoAporte('calcular')}
                                disabled={isLoading}
                            >
                                Quanto preciso aportar?
                            </button>
                            <button
                                type="button"
                                role="radio"
                                aria-checked={modoAporte === 'verificar'}
                                className={`proj-toggle-btn${modoAporte === 'verificar' ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => setModoAporte('verificar')}
                                disabled={isLoading}
                            >
                                Já tenho um aporte em mente
                            </button>
                        </div>
                    </div>

                    {modoAporte === 'verificar' && (
                        <div className="contribute-form-group">
                            <label htmlFor="simAporteMensal">Aporte mensal (R$)</label>
                            <input
                                id="simAporteMensal"
                                type="text"
                                inputMode="numeric"
                                placeholder="0,00"
                                value={aporteMensal}
                                onChange={e => setAporteMensal(maskCurrency(e.target.value))}
                                disabled={isLoading}
                            />
                        </div>
                    )}

                    <div className="proj-form-group">
                        <label>Taxa de juros</label>
                        <div className="proj-toggle-group proj-toggle-group--full" role="radiogroup" aria-label="Fonte da taxa de juros">
                            <button
                                type="button"
                                role="radio"
                                aria-checked={taxaModo === 'selic'}
                                className={`proj-toggle-btn${taxaModo === 'selic' ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => setTaxaModo('selic')}
                                disabled={isLoading}
                            >
                                Selic
                            </button>
                            <button
                                type="button"
                                role="radio"
                                aria-checked={taxaModo === 'cdi'}
                                className={`proj-toggle-btn${taxaModo === 'cdi' ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => setTaxaModo('cdi')}
                                disabled={isLoading}
                            >
                                % CDI
                            </button>
                            <button
                                type="button"
                                role="radio"
                                aria-checked={taxaModo === 'manual'}
                                className={`proj-toggle-btn${taxaModo === 'manual' ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => setTaxaModo('manual')}
                                disabled={isLoading}
                            >
                                Manual
                            </button>
                        </div>
                        {taxaModo === 'manual' && (
                            <input
                                className="proj-taxa-manual-input"
                                type="text"
                                inputMode="decimal"
                                placeholder="Ex: 10,5 (% ao ano)"
                                value={taxaManual}
                                onChange={e => setTaxaManual(e.target.value)}
                                disabled={isLoading}
                            />
                        )}
                        {taxaModo === 'cdi' && (
                            <input
                                className="proj-taxa-manual-input"
                                type="text"
                                inputMode="decimal"
                                placeholder="Ex: 110 (% do CDI)"
                                value={percentualCdi}
                                onChange={e => setPercentualCdi(e.target.value)}
                                disabled={isLoading}
                            />
                        )}
                    </div>

                    <CampoTipoAtivo id="metaModalTipoAtivo" value={tipoAtivo} onChange={setTipoAtivo} disabled={isLoading} />

                    {error && <span className="contribute-error">{error}</span>}

                    <div className="contribute-actions">
                        <button type="button" className="contribute-btn-cancel" onClick={onClose} disabled={isLoading}>
                            Fechar
                        </button>
                        <button type="submit" className="contribute-btn-submit" disabled={isLoading}>
                            {isLoading ? 'Simulando...' : 'Simular'}
                        </button>
                    </div>
                </form>

                {resultado && (
                    <div className={`sim-meta-resultado sim-meta-resultado--${resultado.atinge ? 'ok' : 'falta'}`}>
                        {resultado.aporteMensalNecessario != null ? (
                            <>
                                <span className="sim-meta-resultado-label">Aporte mensal necessário</span>
                                <span className="sim-meta-resultado-valor">{formatCurrency(resultado.aporteMensalNecessario)}</span>
                            </>
                        ) : (
                            <>
                                <span className="sim-meta-resultado-label">
                                    {resultado.atinge ? 'Meta atingida — sobra' : 'Meta não atingida — falta'}
                                </span>
                                <span className="sim-meta-resultado-valor">
                                    {formatCurrency(Math.abs(resultado.diferencaParaMeta))}
                                </span>
                            </>
                        )}
                        <span className="sim-meta-resultado-prazo">
                            Prazo restante: {resultado.prazoMesesRestante} {resultado.prazoMesesRestante === 1 ? 'mês' : 'meses'}
                        </span>
                    </div>
                )}
            </div>
        </div>,
        document.body
    );
}
