import { useState } from 'react';
import { projecaoInvestimentoService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { ProjecaoInvestimentoResponseDto } from '../../types/ProjecaoInvestimento';
import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';
import { FonteTaxaJuros } from '../../types/FonteTaxaJuros';
import { ReajusteAporteModo } from '../../types/ReajusteAporteModo';
import { maskCurrency, parseCurrency, parsePercent } from './calculadoraUtils';
import { ComparadorCenarios } from './ComparadorCenarios';
import { MetaReversaCalculadora } from './MetaReversaCalculadora';
import { RetiradaCalculadora } from './RetiradaCalculadora';
import { CalculadoraFinanciamento } from './CalculadoraFinanciamento';
import { ResultadoProjecaoDetalhado } from './ResultadoProjecaoDetalhado';
import { SeletorTipoAtivo } from './SeletorTipoAtivo';
import './CalculadoraProjecao.css';

type PrazoUnidade = 'anos' | 'meses';
type TaxaModo = 'selic' | 'cdi' | 'manual';
type ModoCalculadora = 'unico' | 'comparar' | 'meta-reversa' | 'retirada' | 'financiamento';
type ReajusteModoUi = 'nenhum' | 'fixo' | 'ipca';

let proximoIdAporteExtra = 0;
interface AporteExtraForm {
    id: string;
    mes: string;
    valor: string;
}

export function CalculadoraProjecao() {
    const [modo, setModo] = useState<ModoCalculadora>('unico');
    const [aporteInicial, setAporteInicial] = useState('');
    const [aporteMensal, setAporteMensal] = useState('');
    const [prazoValor, setPrazoValor] = useState('10');
    const [prazoUnidade, setPrazoUnidade] = useState<PrazoUnidade>('anos');
    const [taxaModo, setTaxaModo] = useState<TaxaModo>('selic');
    const [taxaManual, setTaxaManual] = useState('');
    const [percentualCdi, setPercentualCdi] = useState('100');
    const [tipoAtivo, setTipoAtivo] = useState<TipoAtivoCalculadora>(TipoAtivoCalculadora.TesouroSelic);
    const [aportesExtras, setAportesExtras] = useState<AporteExtraForm[]>([]);
    const [reajusteModo, setReajusteModo] = useState<ReajusteModoUi>('nenhum');
    const [reajusteFixo, setReajusteFixo] = useState('');

    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [resultado, setResultado] = useState<ProjecaoInvestimentoResponseDto | null>(null);

    const prazoMeses = prazoUnidade === 'anos'
        ? Math.round(parseFloat(prazoValor || '0') * 12)
        : Math.round(parseFloat(prazoValor || '0'));

    const adicionarAporteExtra = () =>
        setAportesExtras(prev => [...prev, { id: `extra-${proximoIdAporteExtra++}`, mes: '', valor: '' }]);

    const removerAporteExtra = (id: string) =>
        setAportesExtras(prev => prev.filter(a => a.id !== id));

    const atualizarAporteExtra = (id: string, patch: Partial<AporteExtraForm>) =>
        setAportesExtras(prev => prev.map(a => (a.id === id ? { ...a, ...patch } : a)));

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (!prazoMeses || prazoMeses <= 0) {
            setError('Informe um prazo válido maior que zero.');
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

        const aportesExtrasValidados: { mes: number; valor: number }[] = [];
        for (const extra of aportesExtras) {
            const mes = parseInt(extra.mes, 10);
            const valor = parseCurrency(extra.valor);
            if (!mes || mes <= 0 || mes > prazoMeses || !valor || valor <= 0) {
                setError('Confira os aportes extras: o mês deve estar dentro do prazo e o valor maior que zero.');
                return;
            }
            aportesExtrasValidados.push({ mes, valor });
        }

        const reajusteFixoNumero = reajusteModo === 'fixo' ? parsePercent(reajusteFixo) : null;
        if (reajusteModo === 'fixo' && (reajusteFixoNumero === null || reajusteFixoNumero < 0)) {
            setError('Informe um percentual de reajuste anual válido.');
            return;
        }

        const fonteTaxaJuros = taxaModo === 'selic'
            ? FonteTaxaJuros.Selic
            : taxaModo === 'cdi'
                ? FonteTaxaJuros.PercentualCdi
                : FonteTaxaJuros.Manual;

        const reajusteAporteModo = reajusteModo === 'fixo'
            ? ReajusteAporteModo.PercentualFixo
            : reajusteModo === 'ipca'
                ? ReajusteAporteModo.Ipca
                : ReajusteAporteModo.Nenhum;

        setIsLoading(true);
        try {
            const data = await projecaoInvestimentoService.calcular({
                aporteInicial: parseCurrency(aporteInicial),
                aporteMensal: parseCurrency(aporteMensal),
                prazoMeses,
                fonteTaxaJuros,
                taxaJurosAnualPercentual: taxaModo === 'manual' ? taxaManualNumero! : undefined,
                percentualCdi: taxaModo === 'cdi' ? percentualCdiNumero! : undefined,
                tipoAtivo,
                aportesExtras: aportesExtrasValidados.length > 0 ? aportesExtrasValidados : undefined,
                reajusteAporteModo,
                reajusteAporteAnualPercentual: reajusteModo === 'fixo' ? reajusteFixoNumero! : undefined,
            });
            setResultado(data);
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Não foi possível calcular a projeção. Tente novamente.');
            setResultado(null);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="proj-container">
            <div className="proj-modo-toggle proj-toggle-group" role="radiogroup" aria-label="Modo da calculadora">
                <button
                    type="button"
                    role="radio"
                    aria-checked={modo === 'unico'}
                    className={`proj-toggle-btn${modo === 'unico' ? ' proj-toggle-btn--active' : ''}`}
                    onClick={() => setModo('unico')}
                >
                    Cenário único
                </button>
                <button
                    type="button"
                    role="radio"
                    aria-checked={modo === 'comparar'}
                    className={`proj-toggle-btn${modo === 'comparar' ? ' proj-toggle-btn--active' : ''}`}
                    onClick={() => setModo('comparar')}
                >
                    Comparar cenários
                </button>
                <button
                    type="button"
                    role="radio"
                    aria-checked={modo === 'meta-reversa'}
                    className={`proj-toggle-btn${modo === 'meta-reversa' ? ' proj-toggle-btn--active' : ''}`}
                    onClick={() => setModo('meta-reversa')}
                >
                    Meta reversa
                </button>
                <button
                    type="button"
                    role="radio"
                    aria-checked={modo === 'retirada'}
                    className={`proj-toggle-btn${modo === 'retirada' ? ' proj-toggle-btn--active' : ''}`}
                    onClick={() => setModo('retirada')}
                >
                    Fase de retirada
                </button>
                <button
                    type="button"
                    role="radio"
                    aria-checked={modo === 'financiamento'}
                    className={`proj-toggle-btn${modo === 'financiamento' ? ' proj-toggle-btn--active' : ''}`}
                    onClick={() => setModo('financiamento')}
                >
                    Financiamento
                </button>
            </div>

            {modo === 'comparar' && <ComparadorCenarios />}
            {modo === 'meta-reversa' && <MetaReversaCalculadora />}
            {modo === 'retirada' && <RetiradaCalculadora />}
            {modo === 'financiamento' && <CalculadoraFinanciamento />}

            {modo === 'unico' && (
                <>
                    <form className="proj-form" onSubmit={handleSubmit}>
                        <div className="proj-form-row">
                            <div className="proj-form-group">
                                <label htmlFor="projAporteInicial">Aporte inicial (R$)</label>
                                <input
                                    id="projAporteInicial"
                                    type="text"
                                    inputMode="numeric"
                                    placeholder="0,00"
                                    value={aporteInicial}
                                    onChange={e => setAporteInicial(maskCurrency(e.target.value))}
                                    disabled={isLoading}
                                />
                            </div>
                            <div className="proj-form-group">
                                <label htmlFor="projAporteMensal">Aporte mensal (R$)</label>
                                <input
                                    id="projAporteMensal"
                                    type="text"
                                    inputMode="numeric"
                                    placeholder="0,00"
                                    value={aporteMensal}
                                    onChange={e => setAporteMensal(maskCurrency(e.target.value))}
                                    disabled={isLoading}
                                />
                            </div>
                        </div>

                        <div className="proj-form-group">
                            <label htmlFor="projPrazo">Prazo</label>
                            <div className="proj-prazo-row">
                                <input
                                    id="projPrazo"
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
                                <>
                                    <input
                                        className="proj-taxa-manual-input"
                                        type="text"
                                        inputMode="decimal"
                                        placeholder="Ex: 110 (% do CDI)"
                                        value={percentualCdi}
                                        onChange={e => setPercentualCdi(e.target.value)}
                                        disabled={isLoading}
                                    />
                                    <p className="proj-hint">
                                        O CDI vigente será buscado automaticamente ao calcular. CDB e fundos costumam
                                        ser cotados como um percentual do CDI (ex.: "110% do CDI").
                                    </p>
                                </>
                            )}
                            {taxaModo === 'selic' && (
                                <p className="proj-hint">
                                    A taxa Selic real vigente será buscada automaticamente ao calcular.
                                </p>
                            )}
                        </div>

                        <div className="proj-form-group">
                            <label>Aportes extras (13º, bônus)</label>
                            {aportesExtras.map(extra => (
                                <div key={extra.id} className="proj-aporte-extra-row">
                                    <input
                                        type="number"
                                        min={1}
                                        max={prazoMeses || undefined}
                                        placeholder="Mês"
                                        value={extra.mes}
                                        onChange={e => atualizarAporteExtra(extra.id, { mes: e.target.value })}
                                        disabled={isLoading}
                                    />
                                    <input
                                        type="text"
                                        inputMode="numeric"
                                        placeholder="Valor (R$)"
                                        value={extra.valor}
                                        onChange={e => atualizarAporteExtra(extra.id, { valor: maskCurrency(e.target.value) })}
                                        disabled={isLoading}
                                    />
                                    <button
                                        type="button"
                                        className="proj-aporte-extra-remover"
                                        onClick={() => removerAporteExtra(extra.id)}
                                        disabled={isLoading}
                                        aria-label="Remover aporte extra"
                                    >
                                        ✕
                                    </button>
                                </div>
                            ))}
                            <button
                                type="button"
                                className="proj-aporte-extra-adicionar"
                                onClick={adicionarAporteExtra}
                                disabled={isLoading}
                            >
                                + Adicionar aporte extra
                            </button>
                        </div>

                        <div className="proj-form-group">
                            <label>Reajuste do aporte mensal (a cada 12 meses)</label>
                            <div className="proj-toggle-group proj-toggle-group--full" role="radiogroup" aria-label="Reajuste do aporte mensal">
                                <button
                                    type="button"
                                    role="radio"
                                    aria-checked={reajusteModo === 'nenhum'}
                                    className={`proj-toggle-btn${reajusteModo === 'nenhum' ? ' proj-toggle-btn--active' : ''}`}
                                    onClick={() => setReajusteModo('nenhum')}
                                    disabled={isLoading}
                                >
                                    Sem reajuste
                                </button>
                                <button
                                    type="button"
                                    role="radio"
                                    aria-checked={reajusteModo === 'fixo'}
                                    className={`proj-toggle-btn${reajusteModo === 'fixo' ? ' proj-toggle-btn--active' : ''}`}
                                    onClick={() => setReajusteModo('fixo')}
                                    disabled={isLoading}
                                >
                                    % fixo ao ano
                                </button>
                                <button
                                    type="button"
                                    role="radio"
                                    aria-checked={reajusteModo === 'ipca'}
                                    className={`proj-toggle-btn${reajusteModo === 'ipca' ? ' proj-toggle-btn--active' : ''}`}
                                    onClick={() => setReajusteModo('ipca')}
                                    disabled={isLoading}
                                >
                                    Pelo IPCA
                                </button>
                            </div>
                            {reajusteModo === 'fixo' && (
                                <input
                                    className="proj-taxa-manual-input"
                                    type="text"
                                    inputMode="decimal"
                                    placeholder="Ex: 5 (% ao ano)"
                                    value={reajusteFixo}
                                    onChange={e => setReajusteFixo(e.target.value)}
                                    disabled={isLoading}
                                />
                            )}
                            {reajusteModo === 'ipca' && (
                                <p className="proj-hint">
                                    O IPCA real vigente será buscado automaticamente ao calcular, mesmo com taxa manual.
                                </p>
                            )}
                        </div>

                        <SeletorTipoAtivo tipoAtivo={tipoAtivo} onChange={setTipoAtivo} disabled={isLoading} />

                        {error && <span className="proj-error">{error}</span>}

                        <button type="submit" className="proj-btn-submit" disabled={isLoading}>
                            {isLoading ? 'Calculando...' : 'Calcular projeção'}
                        </button>
                    </form>

                    {resultado && <ResultadoProjecaoDetalhado resultado={resultado} prazoMeses={prazoMeses} />}
                </>
            )}
        </div>
    );
}
