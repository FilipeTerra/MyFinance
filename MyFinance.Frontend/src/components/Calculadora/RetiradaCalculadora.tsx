import { useState } from 'react';
import { retiradaService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { RetiradaResponseDto } from '../../types/Retirada';
import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';
import { FonteTaxaJuros } from '../../types/FonteTaxaJuros';
import { maskCurrency, parseCurrency, parsePercent } from './calculadoraUtils';
import { ResultadoRetiradaDetalhado } from './ResultadoRetiradaDetalhado';
import { SeletorTipoAtivo } from './SeletorTipoAtivo';

type PrazoUnidade = 'anos' | 'meses';
type TaxaModo = 'selic' | 'cdi' | 'manual';
type ObjetivoModo = 'saque' | 'duracao';

export function RetiradaCalculadora() {
    const [objetivo, setObjetivo] = useState<ObjetivoModo>('saque');
    const [saldoInicial, setSaldoInicial] = useState('');
    const [baseCustoInicial, setBaseCustoInicial] = useState('');
    const [prazoValor, setPrazoValor] = useState('30');
    const [prazoUnidade, setPrazoUnidade] = useState<PrazoUnidade>('anos');
    const [saqueMensal, setSaqueMensal] = useState('');
    const [taxaModo, setTaxaModo] = useState<TaxaModo>('selic');
    const [taxaManual, setTaxaManual] = useState('');
    const [percentualCdi, setPercentualCdi] = useState('100');
    const [tipoAtivo, setTipoAtivo] = useState<TipoAtivoCalculadora>(TipoAtivoCalculadora.TesouroSelic);

    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [resultado, setResultado] = useState<RetiradaResponseDto | null>(null);

    const prazoMeses = prazoUnidade === 'anos'
        ? Math.round(parseFloat(prazoValor || '0') * 12)
        : Math.round(parseFloat(prazoValor || '0'));

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setResultado(null);

        const saldoInicialNumero = parseCurrency(saldoInicial);
        const baseCustoNumero = parseCurrency(baseCustoInicial);
        if (!saldoInicialNumero || saldoInicialNumero <= 0) {
            setError('Informe um saldo inicial válido maior que zero.');
            return;
        }
        if (baseCustoNumero < 0 || baseCustoNumero > saldoInicialNumero) {
            setError('A base de custo (total já aportado) deve estar entre zero e o saldo inicial.');
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
            if (objetivo === 'saque') {
                if (!prazoMeses || prazoMeses <= 0) {
                    setError('Informe um prazo de retirada válido maior que zero.');
                    return;
                }
                const data = await retiradaService.calcularSaqueSustentavel({
                    saldoInicial: saldoInicialNumero,
                    baseCustoInicial: baseCustoNumero,
                    prazoMeses,
                    ...taxaConfig,
                });
                setResultado(data);
            } else {
                const saqueMensalNumero = parseCurrency(saqueMensal);
                if (!saqueMensalNumero || saqueMensalNumero <= 0) {
                    setError('Informe um saque mensal válido maior que zero.');
                    return;
                }
                const data = await retiradaService.calcularDuracao({
                    saldoInicial: saldoInicialNumero,
                    baseCustoInicial: baseCustoNumero,
                    saqueMensal: saqueMensalNumero,
                    ...taxaConfig,
                });
                setResultado(data);
            }
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Não foi possível calcular a retirada. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="proj-container">
            <form className="proj-form" onSubmit={handleSubmit}>
                <div className="proj-form-group">
                    <label>O que você quer descobrir?</label>
                    <div className="proj-toggle-group proj-toggle-group--full" role="radiogroup" aria-label="Objetivo da fase de retirada">
                        <button
                            type="button"
                            role="radio"
                            aria-checked={objetivo === 'saque'}
                            className={`proj-toggle-btn${objetivo === 'saque' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setObjetivo('saque')}
                            disabled={isLoading}
                        >
                            Quanto posso sacar por mês?
                        </button>
                        <button
                            type="button"
                            role="radio"
                            aria-checked={objetivo === 'duracao'}
                            className={`proj-toggle-btn${objetivo === 'duracao' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setObjetivo('duracao')}
                            disabled={isLoading}
                        >
                            Quanto tempo meu saldo dura?
                        </button>
                    </div>
                </div>

                <div className="proj-form-row">
                    <div className="proj-form-group">
                        <label htmlFor="retSaldoInicial">Saldo inicial (R$)</label>
                        <input
                            id="retSaldoInicial"
                            type="text"
                            inputMode="numeric"
                            placeholder="0,00"
                            value={saldoInicial}
                            onChange={e => setSaldoInicial(maskCurrency(e.target.value))}
                            disabled={isLoading}
                        />
                    </div>
                    <div className="proj-form-group">
                        <label htmlFor="retBaseCusto">Total já aportado (R$)</label>
                        <input
                            id="retBaseCusto"
                            type="text"
                            inputMode="numeric"
                            placeholder="0,00"
                            value={baseCustoInicial}
                            onChange={e => setBaseCustoInicial(maskCurrency(e.target.value))}
                            disabled={isLoading}
                        />
                    </div>
                </div>
                <p className="proj-hint">
                    O total já aportado (base de custo) é usado para calcular quanto de cada saque é ganho tributável
                    e quanto é apenas devolução do que você mesmo investiu.
                </p>

                {objetivo === 'saque' ? (
                    <div className="proj-form-group">
                        <label htmlFor="retPrazo">Prazo de retirada desejado</label>
                        <div className="proj-prazo-row">
                            <input
                                id="retPrazo"
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
                        <label htmlFor="retSaqueMensal">Saque mensal desejado (R$)</label>
                        <input
                            id="retSaqueMensal"
                            type="text"
                            inputMode="numeric"
                            placeholder="0,00"
                            value={saqueMensal}
                            onChange={e => setSaqueMensal(maskCurrency(e.target.value))}
                            disabled={isLoading}
                        />
                    </div>
                )}

                <div className="proj-form-group">
                    <label>Taxa de retorno na aposentadoria</label>
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
                            placeholder="Ex: 6 (% ao ano)"
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
                            placeholder="Ex: 90 (% do CDI)"
                            value={percentualCdi}
                            onChange={e => setPercentualCdi(e.target.value)}
                            disabled={isLoading}
                        />
                    )}
                </div>

                <SeletorTipoAtivo tipoAtivo={tipoAtivo} onChange={setTipoAtivo} disabled={isLoading} />
                <p className="proj-hint">
                    O IR de cada saque é calculado proporcionalmente ao ganho embutido nele (base de custo
                    amortizada mês a mês), assumindo a alíquota mínima da tabela regressiva — dinheiro em
                    aposentadoria já costuma estar investido há anos.
                </p>

                {error && <span className="proj-error">{error}</span>}

                <button type="submit" className="proj-btn-submit" disabled={isLoading}>
                    {isLoading ? 'Calculando...' : 'Calcular'}
                </button>
            </form>

            {resultado && <ResultadoRetiradaDetalhado resultado={resultado} />}
        </div>
    );
}
