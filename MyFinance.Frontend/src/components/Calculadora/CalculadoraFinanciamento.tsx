import { useMemo, useState } from 'react';
import {
    ComposedChart,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    Legend,
    ResponsiveContainer,
} from 'recharts';
import { financiamentoService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { FinanciamentoResponseDto, ResultadoFinanciamentoDto, TaxaEfetivaResponseDto } from '../../types/Financiamento';
import { maskCurrency, parseCurrency, parsePercent, formatCurrency } from './calculadoraUtils';
import './CalculadoraFinanciamento.css';

type TaxaModo = 'mensal' | 'anual';
type PrazoUnidade = 'anos' | 'meses';
type SistemaVisivel = 'price' | 'sac';

/**
 * Converte uma taxa anual (%) na taxa mensal equivalente (%) por juros
 * compostos: i_mensal = (1 + i_anual)^(1/12) - 1. Usado só para traduzir o
 * que o usuário digita para o formato que o backend espera (taxa mensal,
 * como nos contratos de financiamento reais).
 */
const taxaAnualParaMensal = (taxaAnualPercentual: number): number =>
    (Math.pow(1 + taxaAnualPercentual / 100, 1 / 12) - 1) * 100;

/** Gera e baixa um CSV com o cronograma de amortização dos dois sistemas lado a lado. */
function exportarCronogramaCsv(resultado: FinanciamentoResponseDto) {
    const cabecalho = [
        'Parcela',
        'Price - Valor', 'Price - Juros', 'Price - Amortizacao', 'Price - Saldo Devedor',
        'SAC - Valor', 'SAC - Juros', 'SAC - Amortizacao', 'SAC - Saldo Devedor',
    ];
    const linhas = resultado.price.parcelas.map((p, idx) => {
        const s = resultado.sac.parcelas[idx];
        return [
            p.numero,
            p.valorParcela.toFixed(2), p.juros.toFixed(2), p.amortizacao.toFixed(2), p.saldoDevedor.toFixed(2),
            s.valorParcela.toFixed(2), s.juros.toFixed(2), s.amortizacao.toFixed(2), s.saldoDevedor.toFixed(2),
        ].join(';');
    });
    const csv = [cabecalho.join(';'), ...linhas].join('\n');

    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'cronograma-financiamento.csv';
    link.click();
    URL.revokeObjectURL(url);
}

export function CalculadoraFinanciamento() {
    const [valorFinanciado, setValorFinanciado] = useState('');
    const [taxaModo, setTaxaModo] = useState<TaxaModo>('mensal');
    const [taxaValor, setTaxaValor] = useState('');
    const [prazoValor, setPrazoValor] = useState('48');
    const [prazoUnidade, setPrazoUnidade] = useState<PrazoUnidade>('meses');

    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [resultado, setResultado] = useState<FinanciamentoResponseDto | null>(null);
    const [sistemaVisivel, setSistemaVisivel] = useState<SistemaVisivel>('price');

    // ---------- Conversor APR -> EAR (independente do formulário acima) ----------
    const [taxaNominal, setTaxaNominal] = useState('');
    const [capitalizacoes, setCapitalizacoes] = useState('12');
    const [isLoadingTaxa, setIsLoadingTaxa] = useState(false);
    const [taxaError, setTaxaError] = useState<string | null>(null);
    const [taxaResultado, setTaxaResultado] = useState<TaxaEfetivaResponseDto | null>(null);

    const dadosGrafico = useMemo(() => {
        if (!resultado) return [];
        return resultado.price.parcelas.map((p, idx) => ({
            numero: p.numero,
            saldoPrice: p.saldoDevedor,
            saldoSac: resultado.sac.parcelas[idx].saldoDevedor,
        }));
    }, [resultado]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setResultado(null);

        const valorNumero = parseCurrency(valorFinanciado);
        if (!valorNumero || valorNumero <= 0) {
            setError('Informe um valor financiado válido maior que zero.');
            return;
        }

        const taxaDigitada = parsePercent(taxaValor);
        if (taxaDigitada === null || taxaDigitada < 0) {
            setError('Informe uma taxa de juros válida.');
            return;
        }
        const taxaMensal = taxaModo === 'anual' ? taxaAnualParaMensal(taxaDigitada) : taxaDigitada;

        const prazoDigitado = parseFloat(prazoValor || '0');
        const numParcelas = Math.round(prazoUnidade === 'anos' ? prazoDigitado * 12 : prazoDigitado);
        if (!numParcelas || numParcelas <= 0) {
            setError('Informe um número de parcelas válido maior que zero.');
            return;
        }

        setIsLoading(true);
        try {
            const data = await financiamentoService.simular({
                valorFinanciado: valorNumero,
                taxaJurosMensalPercentual: taxaMensal,
                numParcelas,
            });
            setResultado(data);
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Não foi possível simular o financiamento. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    const handleConverterTaxa = async (e: React.FormEvent) => {
        e.preventDefault();
        setTaxaError(null);
        setTaxaResultado(null);

        const nominal = parsePercent(taxaNominal);
        const m = parseInt(capitalizacoes || '0', 10);
        if (nominal === null || nominal < 0) {
            setTaxaError('Informe uma taxa nominal anual válida.');
            return;
        }
        if (!m || m <= 0) {
            setTaxaError('Informe um número de capitalizações por ano válido.');
            return;
        }

        setIsLoadingTaxa(true);
        try {
            const data = await financiamentoService.calcularTaxaEfetiva({
                taxaNominalAnualPercentual: nominal,
                capitalizacoesPorAno: m,
            });
            setTaxaResultado(data);
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setTaxaError(axiosError.response?.data?.message || 'Não foi possível converter a taxa. Tente novamente.');
        } finally {
            setIsLoadingTaxa(false);
        }
    };

    const schedulesVisiveis: ResultadoFinanciamentoDto | null = resultado
        ? (sistemaVisivel === 'price' ? resultado.price : resultado.sac)
        : null;

    return (
        <div className="proj-container">
            <form className="proj-form" onSubmit={handleSubmit}>
                <div className="proj-form-row">
                    <div className="proj-form-group">
                        <label htmlFor="finValor">Valor financiado (R$)</label>
                        <input
                            id="finValor"
                            type="text"
                            inputMode="numeric"
                            placeholder="0,00"
                            value={valorFinanciado}
                            onChange={e => setValorFinanciado(maskCurrency(e.target.value))}
                            disabled={isLoading}
                        />
                    </div>
                    <div className="proj-form-group">
                        <label htmlFor="finPrazo">Número de parcelas</label>
                        <div className="proj-prazo-row">
                            <input
                                id="finPrazo"
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
                                    aria-checked={prazoUnidade === 'meses'}
                                    className={`proj-toggle-btn${prazoUnidade === 'meses' ? ' proj-toggle-btn--active' : ''}`}
                                    onClick={() => setPrazoUnidade('meses')}
                                    disabled={isLoading}
                                >
                                    Meses
                                </button>
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
                            </div>
                        </div>
                    </div>
                </div>

                <div className="proj-form-group">
                    <label>Taxa de juros do contrato</label>
                    <div className="proj-toggle-group" role="radiogroup" aria-label="Unidade da taxa de juros">
                        <button
                            type="button"
                            role="radio"
                            aria-checked={taxaModo === 'mensal'}
                            className={`proj-toggle-btn${taxaModo === 'mensal' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setTaxaModo('mensal')}
                            disabled={isLoading}
                        >
                            % ao mês
                        </button>
                        <button
                            type="button"
                            role="radio"
                            aria-checked={taxaModo === 'anual'}
                            className={`proj-toggle-btn${taxaModo === 'anual' ? ' proj-toggle-btn--active' : ''}`}
                            onClick={() => setTaxaModo('anual')}
                            disabled={isLoading}
                        >
                            % ao ano
                        </button>
                    </div>
                    <input
                        className="proj-taxa-manual-input"
                        type="text"
                        inputMode="decimal"
                        placeholder={taxaModo === 'mensal' ? 'Ex: 1,5 (% ao mês)' : 'Ex: 15 (% ao ano)'}
                        value={taxaValor}
                        onChange={e => setTaxaValor(e.target.value)}
                        disabled={isLoading}
                    />
                    {taxaModo === 'anual' && (
                        <p className="proj-hint">
                            A taxa anual é convertida para a taxa mensal equivalente antes de simular
                            (juros compostos), já que os sistemas de amortização trabalham com parcelas mensais.
                        </p>
                    )}
                </div>

                {error && <span className="proj-error">{error}</span>}

                <button type="submit" className="proj-btn-submit" disabled={isLoading}>
                    {isLoading ? 'Calculando...' : 'Simular financiamento'}
                </button>
            </form>

            {resultado && (
                <div className="proj-result">
                    <div className="proj-result-stats">
                        <div className="proj-result-stat proj-result-stat--highlight">
                            <span className="proj-result-stat-value">{resultado.sistemaMaisBarato}</span>
                            <span className="proj-result-stat-label">Sistema com menos juros totais</span>
                        </div>
                        <div className="proj-result-stat">
                            <span className="proj-result-stat-value proj-result-stat-value--green">
                                {formatCurrency(resultado.diferencaTotalJuros)}
                            </span>
                            <span className="proj-result-stat-label">Economia escolhendo o sistema mais barato</span>
                        </div>
                        <div className="proj-result-stat">
                            <span className="proj-result-stat-value">{formatCurrency(resultado.price.totalJuros)}</span>
                            <span className="proj-result-stat-label">Total de juros — Price</span>
                        </div>
                        <div className="proj-result-stat">
                            <span className="proj-result-stat-value">{formatCurrency(resultado.sac.totalJuros)}</span>
                            <span className="proj-result-stat-label">Total de juros — SAC</span>
                        </div>
                    </div>

                    <div className="fin-tabela-wrap">
                        <table className="fin-tabela fin-tabela--comparativo">
                            <thead>
                                <tr>
                                    <th>Sistema</th>
                                    <th>1ª parcela</th>
                                    <th>Última parcela</th>
                                    <th>Total pago</th>
                                    <th>Total de juros</th>
                                    <th>Custo efetivo</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr className={resultado.sistemaMaisBarato === 'Price' ? 'fin-row-melhor' : undefined}>
                                    <td>Price</td>
                                    <td>{formatCurrency(resultado.price.primeiraParcela)}</td>
                                    <td>{formatCurrency(resultado.price.ultimaParcela)}</td>
                                    <td>{formatCurrency(resultado.price.totalPago)}</td>
                                    <td>{formatCurrency(resultado.price.totalJuros)}</td>
                                    <td>{resultado.price.custoEfetivoTotalPercentual.toFixed(2)}%</td>
                                </tr>
                                <tr className={resultado.sistemaMaisBarato === 'SAC' ? 'fin-row-melhor' : undefined}>
                                    <td>SAC</td>
                                    <td>{formatCurrency(resultado.sac.primeiraParcela)}</td>
                                    <td>{formatCurrency(resultado.sac.ultimaParcela)}</td>
                                    <td>{formatCurrency(resultado.sac.totalPago)}</td>
                                    <td>{formatCurrency(resultado.sac.totalJuros)}</td>
                                    <td>{resultado.sac.custoEfetivoTotalPercentual.toFixed(2)}%</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <div className="proj-chart">
                        <ResponsiveContainer width="100%" height={280}>
                            <ComposedChart data={dadosGrafico} margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
                                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                                <XAxis dataKey="numero" stroke="#94a3b8" fontSize={12} />
                                <YAxis tickFormatter={(v: number) => formatCurrency(v)} width={90} stroke="#94a3b8" fontSize={12} />
                                <Tooltip
                                    formatter={(value, name) => [formatCurrency(Number(value)), name]}
                                    labelFormatter={(numero) => `Parcela ${numero}`}
                                />
                                <Legend />
                                <Line type="monotone" dataKey="saldoPrice" name="Saldo devedor — Price" stroke="#3b82f6" strokeWidth={2} dot={false} />
                                <Line type="monotone" dataKey="saldoSac" name="Saldo devedor — SAC" stroke="#10b981" strokeWidth={2} dot={false} />
                            </ComposedChart>
                        </ResponsiveContainer>
                    </div>

                    <div className="fin-cronograma-header">
                        <div className="proj-toggle-group" role="radiogroup" aria-label="Sistema exibido no cronograma">
                            <button
                                type="button"
                                role="radio"
                                aria-checked={sistemaVisivel === 'price'}
                                className={`proj-toggle-btn${sistemaVisivel === 'price' ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => setSistemaVisivel('price')}
                            >
                                Cronograma Price
                            </button>
                            <button
                                type="button"
                                role="radio"
                                aria-checked={sistemaVisivel === 'sac'}
                                className={`proj-toggle-btn${sistemaVisivel === 'sac' ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => setSistemaVisivel('sac')}
                            >
                                Cronograma SAC
                            </button>
                        </div>
                        <button type="button" className="fin-btn-export" onClick={() => exportarCronogramaCsv(resultado)}>
                            Exportar CSV (Price + SAC)
                        </button>
                    </div>

                    {schedulesVisiveis && (
                        <div className="fin-tabela-wrap fin-tabela-wrap--scroll">
                            <table className="fin-tabela">
                                <thead>
                                    <tr>
                                        <th>Nº</th>
                                        <th>Parcela</th>
                                        <th>Juros</th>
                                        <th>Amortização</th>
                                        <th>Saldo devedor</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {schedulesVisiveis.parcelas.map(p => (
                                        <tr key={p.numero}>
                                            <td>{p.numero}</td>
                                            <td>{formatCurrency(p.valorParcela)}</td>
                                            <td>{formatCurrency(p.juros)}</td>
                                            <td>{formatCurrency(p.amortizacao)}</td>
                                            <td>{formatCurrency(p.saldoDevedor)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            )}

            <div className="fin-conversor">
                <h3 className="fin-conversor-title">Conversor de taxa nominal (APR) para taxa efetiva anual (EAR)</h3>
                <p className="proj-hint">
                    Taxas de empréstimos costumam ser cotadas como uma taxa nominal anual (APR) capitalizada
                    várias vezes ao ano. A taxa efetiva anual (EAR) mostra o que isso realmente rende:
                    EAR = (1 + APR/m)<sup>m</sup> − 1.
                </p>
                <form className="fin-conversor-form" onSubmit={handleConverterTaxa}>
                    <div className="proj-form-group">
                        <label htmlFor="finTaxaNominal">Taxa nominal anual — APR (%)</label>
                        <input
                            id="finTaxaNominal"
                            type="text"
                            inputMode="decimal"
                            placeholder="Ex: 12"
                            value={taxaNominal}
                            onChange={e => setTaxaNominal(e.target.value)}
                            disabled={isLoadingTaxa}
                        />
                    </div>
                    <div className="proj-form-group">
                        <label htmlFor="finCapitalizacoes">Capitalizações por ano</label>
                        <input
                            id="finCapitalizacoes"
                            type="number"
                            min={1}
                            placeholder="Ex: 12 (mensal)"
                            value={capitalizacoes}
                            onChange={e => setCapitalizacoes(e.target.value)}
                            disabled={isLoadingTaxa}
                        />
                    </div>
                    <button type="submit" className="proj-btn-submit" disabled={isLoadingTaxa}>
                        {isLoadingTaxa ? 'Convertendo...' : 'Converter'}
                    </button>
                </form>
                {taxaError && <span className="proj-error">{taxaError}</span>}
                {taxaResultado && (
                    <div className="fin-conversor-resultado">
                        <span className="proj-result-stat-value">{taxaResultado.taxaEfetivaAnualPercentual.toFixed(4)}%</span>
                        <span className="proj-result-stat-label">Taxa efetiva anual (EAR)</span>
                    </div>
                )}
            </div>
        </div>
    );
}
