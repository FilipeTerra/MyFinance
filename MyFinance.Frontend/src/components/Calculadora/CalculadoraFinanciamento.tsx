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
import { parseCurrency, parsePercent, formatCurrency } from './calculadoraUtils';
import { prazoParaMeses } from './calculadoraValidacao';
import type { PrazoValue } from './calculadoraTypes';
import { CampoMoeda } from './campos/CampoMoeda';
import { CampoPrazo } from './campos/CampoPrazo';
import { CampoTaxaPeriodica, type PeriodicidadeTaxa } from './campos/CampoTaxaPeriodica';
import { FormFooterCalculadora } from './campos/FormFooterCalculadora';
import { ResultadoSecao } from './campos/ResultadoSecao';
import { SegmentedControl, Colapsavel } from '../Shared/ui';
import { useResultadoFoco } from '../../hooks/useResultadoFoco';
import { useErrosFormulario } from '../../hooks/useErrosFormulario';
import './CalculadoraFinanciamento.css';

type SistemaVisivel = 'price' | 'sac';
type CampoErro = 'valor' | 'taxa' | 'prazo';
const ID_POR_CAMPO: Record<CampoErro, string> = { valor: 'finValor', taxa: 'finTaxa-taxa-valor', prazo: 'finPrazo' };

const PARCELAS_INICIAIS_VISIVEIS = 60;

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
    const [periodicidade, setPeriodicidade] = useState<PeriodicidadeTaxa>('mensal');
    const [taxaValor, setTaxaValor] = useState('');
    const [prazo, setPrazo] = useState<PrazoValue>({ valor: '48', unidade: 'meses' });

    const [isLoading, setIsLoading] = useState(false);
    const [resultado, setResultado] = useState<FinanciamentoResponseDto | null>(null);
    const [sistemaVisivel, setSistemaVisivel] = useState<SistemaVisivel>('price');
    const [mostrarTodasParcelas, setMostrarTodasParcelas] = useState(false);
    const { erros, erroGeral, limpar, limparTudo, setErroGeral, definirEFocar } = useErrosFormulario<CampoErro>();
    const resultadoRef = useResultadoFoco(resultado);

    // ---------- Conversor APR -> EAR, embutido no campo de taxa ----------
    const [taxaNominal, setTaxaNominal] = useState('');
    const [capitalizacoes, setCapitalizacoes] = useState('12');
    const [isLoadingTaxa, setIsLoadingTaxa] = useState(false);
    const [taxaErroConversor, setTaxaErroConversor] = useState<string | null>(null);
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
        limparTudo();
        setMostrarTodasParcelas(false);

        const novosErros: Partial<Record<CampoErro, string>> = {};

        const valorNumero = parseCurrency(valorFinanciado);
        if (!valorNumero || valorNumero <= 0) novosErros.valor = 'Informe um valor financiado válido maior que zero.';

        const taxaDigitada = parsePercent(taxaValor);
        if (taxaDigitada === null || taxaDigitada < 0) novosErros.taxa = 'Informe uma taxa de juros válida.';

        const numParcelas = prazoParaMeses(prazo);
        if (!numParcelas || numParcelas <= 0) novosErros.prazo = 'Informe um número de parcelas válido maior que zero.';

        if (Object.keys(novosErros).length > 0) {
            definirEFocar(novosErros, ID_POR_CAMPO);
            return;
        }

        const taxaMensal = periodicidade === 'anual' ? taxaAnualParaMensal(taxaDigitada!) : taxaDigitada!;

        setIsLoading(true);
        setResultado(null);
        try {
            const data = await financiamentoService.simular({
                valorFinanciado: valorNumero,
                taxaJurosMensalPercentual: taxaMensal,
                numParcelas,
            });
            setResultado(data);
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setErroGeral(axiosError.response?.data?.message || 'Não foi possível simular o financiamento. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    const handleConverterTaxa = async (e: React.FormEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setTaxaErroConversor(null);
        setTaxaResultado(null);

        const nominal = parsePercent(taxaNominal);
        const m = parseInt(capitalizacoes || '0', 10);
        if (nominal === null || nominal < 0) {
            setTaxaErroConversor('Informe uma taxa nominal anual válida.');
            return;
        }
        if (!m || m <= 0) {
            setTaxaErroConversor('Informe um número de capitalizações por ano válido.');
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
            setTaxaErroConversor(axiosError.response?.data?.message || 'Não foi possível converter a taxa. Tente novamente.');
        } finally {
            setIsLoadingTaxa(false);
        }
    };

    const usarTaxaConvertida = () => {
        if (!taxaResultado) return;
        setPeriodicidade('anual');
        setTaxaValor(String(taxaResultado.taxaEfetivaAnualPercentual).replace('.', ','));
        limpar('taxa');
    };

    const schedulesVisiveis: ResultadoFinanciamentoDto | null = resultado
        ? (sistemaVisivel === 'price' ? resultado.price : resultado.sac)
        : null;
    const parcelasExibidas = schedulesVisiveis && !mostrarTodasParcelas
        ? schedulesVisiveis.parcelas.slice(0, PARCELAS_INICIAIS_VISIVEIS)
        : schedulesVisiveis?.parcelas;
    const temMaisParcelas = (schedulesVisiveis?.parcelas.length ?? 0) > PARCELAS_INICIAIS_VISIVEIS;

    return (
        <div className="proj-container">
            <form className="proj-form" onSubmit={handleSubmit}>
                <div className="proj-form-row">
                    <CampoMoeda
                        id="finValor"
                        label="Valor financiado (R$)"
                        value={valorFinanciado}
                        onChange={v => { setValorFinanciado(v); limpar('valor'); }}
                        disabled={isLoading}
                        erro={erros.valor}
                    />
                    <CampoPrazo
                        id="finPrazo"
                        label="Número de parcelas"
                        value={prazo}
                        onChange={v => { setPrazo(v); limpar('prazo'); }}
                        ordemUnidades={['meses', 'anos']}
                        disabled={isLoading}
                        erro={erros.prazo}
                    />
                </div>

                <CampoTaxaPeriodica
                    id="finTaxa-taxa-valor"
                    periodicidade={periodicidade}
                    onChangePeriodicidade={setPeriodicidade}
                    valor={taxaValor}
                    onChangeValor={v => { setTaxaValor(v); limpar('taxa'); }}
                    disabled={isLoading}
                    erro={erros.taxa}
                    hint={periodicidade === 'anual' ? 'A taxa anual é convertida para a mensal equivalente antes de simular (juros compostos).' : undefined}
                />

                <Colapsavel titulo="Não sei a taxa efetiva — converter de taxa nominal (APR)">
                    <p className="campo-hint">
                        Taxas de empréstimos costumam ser cotadas como uma taxa nominal anual (APR) capitalizada
                        várias vezes ao ano. A taxa efetiva anual (EAR) mostra o que isso realmente rende:
                        EAR = (1 + APR/m)<sup>m</sup> − 1.
                    </p>
                    <div className="fin-conversor-form">
                        <div className="campo-form-group">
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
                        <div className="campo-form-group">
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
                        <button type="button" className="campo-btn-submit campo-btn-submit--secundaria" onClick={handleConverterTaxa} disabled={isLoadingTaxa}>
                            {isLoadingTaxa ? 'Convertendo...' : 'Converter'}
                        </button>
                    </div>
                    {taxaErroConversor && <span className="campo-erro">{taxaErroConversor}</span>}
                    {taxaResultado && (
                        <div className="fin-conversor-resultado">
                            <div className="fin-conversor-resultado-valor">
                                <span className="proj-result-stat-value">{taxaResultado.taxaEfetivaAnualPercentual.toFixed(4)}%</span>
                                <span className="proj-result-stat-label">Taxa efetiva anual (EAR)</span>
                            </div>
                            <button type="button" className="campo-btn-submit campo-btn-submit--secundaria" onClick={usarTaxaConvertida}>
                                Usar esta taxa
                            </button>
                        </div>
                    )}
                </Colapsavel>

                <FormFooterCalculadora erro={erroGeral} isLoading={isLoading} rotulo="Simular financiamento" />
            </form>

            {resultado && (
                <ResultadoSecao resultadoRef={resultadoRef}>
                    <div className="proj-result-stats">
                        <div className="proj-result-stat proj-result-stat--highlight">
                            <span className="proj-result-stat-value proj-result-stat-value--texto">{resultado.sistemaMaisBarato}</span>
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
                    <p className="calc-resultado-eco">
                        O sistema <strong>{resultado.sistemaMaisBarato}</strong> sai {formatCurrency(resultado.diferencaTotalJuros)} mais barato no total.
                    </p>

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
                        <SegmentedControl
                            value={sistemaVisivel}
                            onChange={setSistemaVisivel}
                            ariaLabel="Sistema exibido no cronograma"
                            opcoes={[
                                { valor: 'price', rotulo: 'Cronograma Price' },
                                { valor: 'sac', rotulo: 'Cronograma SAC' },
                            ]}
                        />
                        <button type="button" className="fin-btn-export" onClick={() => exportarCronogramaCsv(resultado)}>
                            Exportar CSV (Price + SAC)
                        </button>
                    </div>

                    {parcelasExibidas && (
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
                                    {parcelasExibidas.map(p => (
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
                    {temMaisParcelas && !mostrarTodasParcelas && (
                        <button type="button" className="fin-btn-export" onClick={() => setMostrarTodasParcelas(true)}>
                            Mostrar todas as {schedulesVisiveis?.parcelas.length} parcelas
                        </button>
                    )}
                </ResultadoSecao>
            )}
        </div>
    );
}
