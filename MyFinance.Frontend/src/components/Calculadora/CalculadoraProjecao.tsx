import { useState } from 'react';
import { projecaoInvestimentoService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { ProjecaoInvestimentoResponseDto } from '../../types/ProjecaoInvestimento';
import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';
import { ReajusteAporteModo } from '../../types/ReajusteAporteModo';
import { maskCurrency, parseCurrency, parsePercent } from './calculadoraUtils';
import { prazoParaMeses, validarPrazo, validarTaxaRendimento, parametrosTaxa } from './calculadoraValidacao';
import { BASE_APORTE_PRAZO_INICIAL, type BaseAportePrazo, type ModoCalculadora, type ReajusteModoUi, type TaxaRendimentoValue } from './calculadoraTypes';
import { ComparadorCenarios } from './ComparadorCenarios';
import { MetaReversaCalculadora } from './MetaReversaCalculadora';
import { RetiradaCalculadora } from './RetiradaCalculadora';
import { CalculadoraFinanciamento } from './CalculadoraFinanciamento';
import { ResultadoProjecaoDetalhado } from './ResultadoProjecaoDetalhado';
import { CampoMoeda } from './campos/CampoMoeda';
import { CampoPrazo } from './campos/CampoPrazo';
import { CampoTaxaRendimento } from './campos/CampoTaxaRendimento';
import { CampoTipoAtivo } from './campos/CampoTipoAtivo';
import { FormFooterCalculadora } from './campos/FormFooterCalculadora';
import { ResultadoSecao } from './campos/ResultadoSecao';
import { SegmentedControl, Colapsavel } from '../Shared/ui';
import { useResultadoFoco } from '../../hooks/useResultadoFoco';
import { useErrosFormulario } from '../../hooks/useErrosFormulario';
import './CalculadoraProjecao.css';

interface ModoConfig {
    id: ModoCalculadora;
    rotulo: string;
    icone: string;
    descricao: string;
}

/** Config do seletor de modos — antes eram 5 blocos de botão copiados à mão, sem descrição nenhuma para quem chega pela primeira vez. */
const MODOS: ModoConfig[] = [
    { id: 'unico', rotulo: 'Cenário único', icone: '📈', descricao: 'Quanto seu dinheiro rende investindo um valor por mês.' },
    { id: 'comparar', rotulo: 'Comparar', icone: '⚖️', descricao: 'Coloque até 4 tipos de investimento lado a lado, com o mesmo aporte e prazo.' },
    { id: 'meta-reversa', rotulo: 'Meta reversa', icone: '🎯', descricao: 'Você sabe quanto quer ter. Descubra quanto precisa aportar — ou quanto tempo vai levar.' },
    { id: 'retirada', rotulo: 'Fase de retirada', icone: '🏖️', descricao: 'Já acumulou. Descubra quanto pode sacar por mês e por quanto tempo o dinheiro dura.' },
    { id: 'financiamento', rotulo: 'Financiamento', icone: '🏦', descricao: 'Compare Price e SAC, veja o custo total e a tabela de parcelas.' },
];

let proximoIdAporteExtra = 0;
interface AporteExtraForm {
    id: string;
    mes: string;
    valor: string;
}

type CampoErro = 'prazo' | 'taxa' | 'aportesExtras' | 'reajuste';
const ID_POR_CAMPO: Record<CampoErro, string> = {
    prazo: 'projPrazo',
    taxa: 'proj-taxa-valor',
    aportesExtras: 'projAportesExtras',
    reajuste: 'proj-reajuste-valor',
};

/**
 * Container da Calculadora: seletor de modos + os 5 modos, todos sempre
 * montados (escondidos com `hidden` em vez de desmontados) para que trocar
 * de modo nunca apague o que o usuário já digitou em nenhum deles. Aporte
 * inicial/mensal e prazo — os únicos três campos com o mesmo significado em
 * "Cenário único" e "Comparar cenários" — ficam sincronizados entre os dois.
 */
export function CalculadoraProjecao() {
    const [modo, setModo] = useState<ModoCalculadora>('unico');
    const [base, setBase] = useState<BaseAportePrazo>(BASE_APORTE_PRAZO_INICIAL);
    const [taxa, setTaxa] = useState<TaxaRendimentoValue>({ modo: 'selic', taxaManual: '', percentualCdi: '100' });
    const [tipoAtivo, setTipoAtivo] = useState<TipoAtivoCalculadora>(TipoAtivoCalculadora.TesouroSelic);
    const [aportesExtras, setAportesExtras] = useState<AporteExtraForm[]>([]);
    const [reajusteModo, setReajusteModo] = useState<ReajusteModoUi>('nenhum');
    const [reajusteFixo, setReajusteFixo] = useState('');

    const [isLoading, setIsLoading] = useState(false);
    const [resultado, setResultado] = useState<ProjecaoInvestimentoResponseDto | null>(null);
    const { erros, erroGeral, limpar, limparTudo, setErroGeral, definirEFocar } = useErrosFormulario<CampoErro>();
    const resultadoRef = useResultadoFoco(resultado);

    const prazoMeses = prazoParaMeses(base.prazo);

    const adicionarAporteExtra = () =>
        setAportesExtras(prev => [...prev, { id: `extra-${proximoIdAporteExtra++}`, mes: '', valor: '' }]);
    const removerAporteExtra = (id: string) => setAportesExtras(prev => prev.filter(a => a.id !== id));
    const atualizarAporteExtra = (id: string, patch: Partial<AporteExtraForm>) =>
        setAportesExtras(prev => prev.map(a => (a.id === id ? { ...a, ...patch } : a)));

    const opcoesAvancadasAtivas = (aportesExtras.length > 0 ? 1 : 0) + (reajusteModo !== 'nenhum' ? 1 : 0);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        limparTudo();

        const novosErros: Partial<Record<CampoErro, string>> = {};

        const erroPrazo = validarPrazo(base.prazo);
        if (erroPrazo) novosErros.prazo = erroPrazo;

        const erroTaxa = validarTaxaRendimento(taxa);
        if (erroTaxa) novosErros.taxa = erroTaxa;

        const aportesExtrasValidados: { mes: number; valor: number }[] = [];
        for (const extra of aportesExtras) {
            const mes = parseInt(extra.mes, 10);
            const valor = parseCurrency(extra.valor);
            if (!mes || mes <= 0 || mes > prazoMeses || !valor || valor <= 0) {
                novosErros.aportesExtras = 'Confira os aportes extras: o mês deve estar dentro do prazo e o valor maior que zero.';
                break;
            }
            aportesExtrasValidados.push({ mes, valor });
        }

        const reajusteFixoNumero = reajusteModo === 'fixo' ? parsePercent(reajusteFixo) : null;
        if (reajusteModo === 'fixo' && (reajusteFixoNumero === null || reajusteFixoNumero < 0)) {
            novosErros.reajuste = 'Informe um percentual de reajuste anual válido.';
        }

        if (!parseCurrency(base.aporteInicial) && !parseCurrency(base.aporteMensal)) {
            setErroGeral('Informe um aporte inicial ou um aporte mensal para simular.');
        }

        if (Object.keys(novosErros).length > 0) {
            definirEFocar(novosErros, ID_POR_CAMPO);
            return;
        }
        if (!parseCurrency(base.aporteInicial) && !parseCurrency(base.aporteMensal)) return;

        const reajusteAporteModo = reajusteModo === 'fixo'
            ? ReajusteAporteModo.PercentualFixo
            : reajusteModo === 'ipca'
                ? ReajusteAporteModo.Ipca
                : ReajusteAporteModo.Nenhum;

        setIsLoading(true);
        setResultado(null);
        try {
            const data = await projecaoInvestimentoService.calcular({
                aporteInicial: parseCurrency(base.aporteInicial),
                aporteMensal: parseCurrency(base.aporteMensal),
                prazoMeses,
                ...parametrosTaxa(taxa),
                tipoAtivo,
                aportesExtras: aportesExtrasValidados.length > 0 ? aportesExtrasValidados : undefined,
                reajusteAporteModo,
                reajusteAporteAnualPercentual: reajusteModo === 'fixo' ? reajusteFixoNumero! : undefined,
            });
            setResultado(data);
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setErroGeral(axiosError.response?.data?.message || 'Não foi possível calcular a projeção. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    const modoAtivo = MODOS.find(m => m.id === modo)!;

    return (
        <div className="proj-container">
            <div className="calc-modo-header">
                <SegmentedControl
                    value={modo}
                    onChange={setModo}
                    ariaLabel="Modo da calculadora"
                    variante="tabs"
                    rolavel
                    opcoes={MODOS.map(m => ({ valor: m.id, rotulo: m.rotulo, icone: m.icone }))}
                />
                <p className="calc-modo-descricao">{modoAtivo.descricao}</p>
            </div>

            <div hidden={modo !== 'comparar'}>
                <ComparadorCenarios base={base} onBaseChange={setBase} />
            </div>
            <div hidden={modo !== 'meta-reversa'}>
                <MetaReversaCalculadora />
            </div>
            <div hidden={modo !== 'retirada'}>
                <RetiradaCalculadora />
            </div>
            <div hidden={modo !== 'financiamento'}>
                <CalculadoraFinanciamento />
            </div>

            <div hidden={modo !== 'unico'}>
                <form className="proj-form" onSubmit={handleSubmit}>
                    <div className="proj-form-row">
                        <CampoMoeda
                            id="projAporteInicial"
                            label="Aporte inicial (R$)"
                            value={base.aporteInicial}
                            onChange={v => setBase({ ...base, aporteInicial: v })}
                            disabled={isLoading}
                        />
                        <CampoMoeda
                            id="projAporteMensal"
                            label="Aporte mensal (R$)"
                            value={base.aporteMensal}
                            onChange={v => setBase({ ...base, aporteMensal: v })}
                            disabled={isLoading}
                        />
                    </div>

                    <CampoPrazo
                        id="projPrazo"
                        value={base.prazo}
                        onChange={prazo => { setBase({ ...base, prazo }); limpar('prazo'); }}
                        disabled={isLoading}
                        erro={erros.prazo}
                    />

                    <CampoTaxaRendimento
                        idPrefix="proj"
                        value={taxa}
                        onChange={v => { setTaxa(v); limpar('taxa'); }}
                        disabled={isLoading}
                        erro={erros.taxa}
                    />

                    <CampoTipoAtivo id="projTipoAtivo" value={tipoAtivo} onChange={setTipoAtivo} disabled={isLoading} />

                    <Colapsavel
                        titulo="Opções avançadas"
                        selo={opcoesAvancadasAtivas > 0 ? `${opcoesAvancadasAtivas} ativas` : undefined}
                    >
                        <div className="campo-form-group" id="projAportesExtras">
                            <label>Aportes extras (13º, bônus)</label>
                            {aportesExtras.map(extra => (
                                <div key={extra.id} className="proj-aporte-extra-row">
                                    <input
                                        type="number"
                                        min={1}
                                        max={prazoMeses || undefined}
                                        placeholder="Mês"
                                        value={extra.mes}
                                        onChange={e => { atualizarAporteExtra(extra.id, { mes: e.target.value }); limpar('aportesExtras'); }}
                                        disabled={isLoading}
                                    />
                                    <input
                                        type="text"
                                        inputMode="numeric"
                                        placeholder="Valor (R$)"
                                        value={extra.valor}
                                        onChange={e => { atualizarAporteExtra(extra.id, { valor: maskCurrency(e.target.value) }); limpar('aportesExtras'); }}
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
                            <button type="button" className="proj-aporte-extra-adicionar" onClick={adicionarAporteExtra} disabled={isLoading}>
                                + Adicionar aporte extra
                            </button>
                            {erros.aportesExtras && <span className="campo-erro">{erros.aportesExtras}</span>}
                        </div>

                        <div className="campo-form-group">
                            <label>Reajuste do aporte mensal (a cada 12 meses)</label>
                            <SegmentedControl
                                value={reajusteModo}
                                onChange={v => { setReajusteModo(v); limpar('reajuste'); }}
                                ariaLabel="Reajuste do aporte mensal"
                                full
                                opcoes={[
                                    { valor: 'nenhum', rotulo: 'Sem reajuste' },
                                    { valor: 'fixo', rotulo: '% fixo ao ano' },
                                    { valor: 'ipca', rotulo: 'Pelo IPCA' },
                                ]}
                                disabled={isLoading}
                            />
                            {reajusteModo === 'fixo' && (
                                <input
                                    id="proj-reajuste-valor"
                                    className="campo-taxa-manual-input"
                                    type="text"
                                    inputMode="decimal"
                                    placeholder="Ex: 5 (% ao ano)"
                                    value={reajusteFixo}
                                    onChange={e => { setReajusteFixo(e.target.value); limpar('reajuste'); }}
                                    disabled={isLoading}
                                    aria-invalid={!!erros.reajuste}
                                />
                            )}
                            {reajusteModo === 'ipca' && (
                                <p className="campo-hint">O IPCA real vigente será buscado automaticamente ao calcular, mesmo com taxa manual.</p>
                            )}
                            {erros.reajuste && <span className="campo-erro">{erros.reajuste}</span>}
                        </div>
                    </Colapsavel>

                    <FormFooterCalculadora erro={erroGeral} isLoading={isLoading} rotulo="Calcular projeção" />
                </form>

                {resultado && (
                    <ResultadoSecao resultadoRef={resultadoRef}>
                        <ResultadoProjecaoDetalhado resultado={resultado} prazoMeses={prazoMeses} />
                    </ResultadoSecao>
                )}
            </div>
        </div>
    );
}
