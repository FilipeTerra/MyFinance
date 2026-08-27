import { useState } from 'react';
import { metaReversaService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { AporteNecessarioResponseDto, PrazoNecessarioResponseDto } from '../../types/MetaReversa';
import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';
import { FonteTaxaJuros } from '../../types/FonteTaxaJuros';
import { maskCurrency, parseCurrency, parsePercent, formatCurrency } from './calculadoraUtils';
import { ResultadoProjecaoDetalhado } from './ResultadoProjecaoDetalhado';
import { SeletorTipoAtivo } from './SeletorTipoAtivo';

type PrazoUnidade = 'anos' | 'meses';
type TaxaModo = 'selic' | 'cdi' | 'manual';
type ObjetivoModo = 'aporte' | 'prazo';

const formatPrazo = (meses: number): string => {
    const anos = Math.floor(meses / 12);
    const restoMeses = meses % 12;
    if (anos === 0) return `${meses} ${meses === 1 ? 'mês' : 'meses'}`;
    if (restoMeses === 0) return `${anos} ${anos === 1 ? 'ano' : 'anos'}`;
    return `${anos} ${anos === 1 ? 'ano' : 'anos'} e ${restoMeses} ${restoMeses === 1 ? 'mês' : 'meses'}`;
};

export function MetaReversaCalculadora() {
    const [objetivo, setObjetivo] = useState<ObjetivoModo>('aporte');
    const [aporteInicial, setAporteInicial] = useState('');
    const [valorAlvo, setValorAlvo] = useState('');
    const [prazoValor, setPrazoValor] = useState('10');
    const [prazoUnidade, setPrazoUnidade] = useState<PrazoUnidade>('anos');
    const [aporteMensal, setAporteMensal] = useState('');
    const [taxaModo, setTaxaModo] = useState<TaxaModo>('selic');
    const [taxaManual, setTaxaManual] = useState('');
    const [percentualCdi, setPercentualCdi] = useState('100');
    const [tipoAtivo, setTipoAtivo] = useState<TipoAtivoCalculadora>(TipoAtivoCalculadora.TesouroSelic);

    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [resultadoAporte, setResultadoAporte] = useState<AporteNecessarioResponseDto | null>(null);
    const [resultadoPrazo, setResultadoPrazo] = useState<PrazoNecessarioResponseDto | null>(null);

    const prazoMeses = prazoUnidade === 'anos'
        ? Math.round(parseFloat(prazoValor || '0') * 12)
        : Math.round(parseFloat(prazoValor || '0'));

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setResultadoAporte(null);
        setResultadoPrazo(null);

        const valorAlvoNumero = parseCurrency(valorAlvo);
        if (!valorAlvoNumero || valorAlvoNumero <= 0) {
            setError('Informe um valor-alvo válido maior que zero.');
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

        const taxaConfig = {
            fonteTaxaJuros,
            taxaJurosAnualPercentual: taxaModo === 'manual' ? taxaManualNumero! : undefined,
            percentualCdi: taxaModo === 'cdi' ? percentualCdiNumero! : undefined,
            tipoAtivo,
        };

        setIsLoading(true);
        try {
            if (objetivo === 'aporte') {
                if (!prazoMeses || prazoMeses <= 0) {
                    setError('Informe um prazo válido maior que zero.');
                    return;
                }
                const data = await metaReversaService.calcularAporteNecessario({
                    aporteInicial: parseCurrency(aporteInicial),
                    valorAlvo: valorAlvoNumero,
                    prazoMeses,
                    ...taxaConfig,
                });
                setResultadoAporte(data);
            } else {
                const aporteMensalNumero = parseCurrency(aporteMensal);
                if (!aporteMensalNumero || aporteMensalNumero <= 0) {
                    setError('Informe um aporte mensal válido maior que zero.');
                    return;
                }
                const data = await metaReversaService.calcularPrazoNecessario({
                    aporteInicial: parseCurrency(aporteInicial),
                    aporteMensal: aporteMensalNumero,
                    valorAlvo: valorAlvoNumero,
                    ...taxaConfig,
                });
                setResultadoPrazo(data);
            }
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Não foi possível calcular a meta reversa. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="proj-container">
            <form className="proj-form" onSubmit={handleSubmit}>
                <div className="proj-form-group">
                    <label>O que você quer descobrir?</label>
                    <div className="proj-toggle-group proj-toggle-group--full" role="radiogroup" aria-label="Objetivo da meta reversa">
                        <button
                            type="button"
                            role="radio"
                            aria-checked={objetivo === 'aporte'}
                            className={`proj-toggle-btn${objetivo === 'aporte' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setObjetivo('aporte')}
                            disabled={isLoading}
                        >
                            Quanto preciso aportar por mês?
                        </button>
                        <button
                            type="button"
                            role="radio"
                            aria-checked={objetivo === 'prazo'}
                            className={`proj-toggle-btn${objetivo === 'prazo' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setObjetivo('prazo')}
                            disabled={isLoading}
                        >
                            Quanto tempo vou levar?
                        </button>
                    </div>
                </div>

                <div className="proj-form-row">
                    <div className="proj-form-group">
                        <label htmlFor="metaAporteInicial">Aporte inicial (R$)</label>
                        <input
                            id="metaAporteInicial"
                            type="text"
                            inputMode="numeric"
                            placeholder="0,00"
                            value={aporteInicial}
                            onChange={e => setAporteInicial(maskCurrency(e.target.value))}
                            disabled={isLoading}
                        />
                    </div>
                    <div className="proj-form-group">
                        <label htmlFor="metaValorAlvo">Valor-alvo líquido (R$)</label>
                        <input
                            id="metaValorAlvo"
                            type="text"
                            inputMode="numeric"
                            placeholder="0,00"
                            value={valorAlvo}
                            onChange={e => setValorAlvo(maskCurrency(e.target.value))}
                            disabled={isLoading}
                        />
                    </div>
                </div>

                {objetivo === 'aporte' ? (
                    <div className="proj-form-group">
                        <label htmlFor="metaPrazo">Prazo</label>
                        <div className="proj-prazo-row">
                            <input
                                id="metaPrazo"
                                type="number"
                                min={1}
                                value={prazoValor}
                                onChange={e => setPrazoValor(e.target.value)}
                                disabled={isLoading}
                            />
                            <div className="proj-toggle-group" role="radiogroup" aria-label="Unidade do prazo">
                                <button
                                    type="button"
                                    role="radio"
                                    aria-checked={prazoUnidade === 'anos'}
                                    className={`proj-toggle-btn${prazoUnidade === 'anos' ? ' proj-toggle-btn--active' : ''}`}
                                    onClick={() => setPrazoUnidade('anos')}
                                    disabled={isLoading}
                                >
                                    Anos
                                </button>
                                <button
                                    type="button"
                                    role="radio"
                                    aria-checked={prazoUnidade === 'meses'}
                                    className={`proj-toggle-btn${prazoUnidade === 'meses' ? ' proj-toggle-btn--active' : ''}`}
                                    onClick={() => setPrazoUnidade('meses')}
                                    disabled={isLoading}
                                >
                                    Meses
                                </button>
                            </div>
                        </div>
                    </div>
                ) : (
                    <div className="proj-form-group">
                        <label htmlFor="metaAporteMensal">Aporte mensal (R$)</label>
                        <input
                            id="metaAporteMensal"
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
                            Tesouro Direto (Selic atual)
                        </button>
                        <button
                            type="button"
                            role="radio"
                            aria-checked={taxaModo === 'cdi'}
                            className={`proj-toggle-btn${taxaModo === 'cdi' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setTaxaModo('cdi')}
                            disabled={isLoading}
                        >
                            % do CDI
                        </button>
                        <button
                            type="button"
                            role="radio"
                            aria-checked={taxaModo === 'manual'}
                            className={`proj-toggle-btn${taxaModo === 'manual' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setTaxaModo('manual')}
                            disabled={isLoading}
                        >
                            Taxa manual
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

                <SeletorTipoAtivo tipoAtivo={tipoAtivo} onChange={setTipoAtivo} disabled={isLoading} />

                {error && <span className="proj-error">{error}</span>}

                <button type="submit" className="proj-btn-submit" disabled={isLoading}>
                    {isLoading ? 'Calculando...' : 'Calcular'}
                </button>
            </form>

            {resultadoAporte && (
                <div className="proj-result">
                    <div className="proj-result-stats">
                        <div className="proj-result-stat proj-result-stat--highlight">
                            <span className="proj-result-stat-value">{formatCurrency(resultadoAporte.aporteMensalNecessario)}</span>
                            <span className="proj-result-stat-label">Aporte mensal necessário</span>
                        </div>
                    </div>
                    <ResultadoProjecaoDetalhado resultado={resultadoAporte.projecao} prazoMeses={prazoMeses} />
                </div>
            )}

            {resultadoPrazo && (
                resultadoPrazo.atingivel && resultadoPrazo.projecao && resultadoPrazo.prazoMesesNecessario ? (
                    <div className="proj-result">
                        <div className="proj-result-stats">
                            <div className="proj-result-stat proj-result-stat--highlight">
                                <span className="proj-result-stat-value">{formatPrazo(resultadoPrazo.prazoMesesNecessario)}</span>
                                <span className="proj-result-stat-label">Prazo necessário</span>
                            </div>
                        </div>
                        <ResultadoProjecaoDetalhado
                            resultado={resultadoPrazo.projecao}
                            prazoMeses={resultadoPrazo.prazoMesesNecessario}
                        />
                    </div>
                ) : (
                    <p className="proj-error" style={{ display: 'block' }}>
                        Com esse aporte e taxa, a meta não é atingível em até 50 anos. Aumente o aporte mensal ou
                        revise o valor-alvo.
                    </p>
                )
            )}
        </div>
    );
}
