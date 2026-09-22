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
import type {
    AmortizacaoExtraAvulsaDto,
    AmortizarVsInvestirResponseDto,
    FinanciamentoResponseDto,
    ParcelaFinanciamentoDto,
    ResultadoFinanciamentoDto,
    TaxaEfetivaResponseDto,
} from '../../types/Financiamento';
import { ROTULO_FAIXA_MCMV } from '../../types/Financiamento';
import {
    ModoAmortizacaoExtra,
    ROTULO_SISTEMA,
    SistemaAmortizacao,
    RecomendacaoFinanceira,
    ROTULO_RECOMENDACAO,
} from '../../types/SistemaAmortizacao';
import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';
import { parseCurrency, parsePercent, formatCurrency, maskCurrency } from './calculadoraUtils';
import { prazoParaMeses, formatPrazo, parametrosTaxa, validarTaxaRendimento } from './calculadoraValidacao';
import type { PrazoValue, TaxaRendimentoValue } from './calculadoraTypes';
import { CampoMoeda } from './campos/CampoMoeda';
import { CampoPrazo } from './campos/CampoPrazo';
import { CampoTaxaPeriodica, type PeriodicidadeTaxa } from './campos/CampoTaxaPeriodica';
import { CampoTaxaRendimento } from './campos/CampoTaxaRendimento';
import { CampoTipoAtivo } from './campos/CampoTipoAtivo';
import { FormFooterCalculadora } from './campos/FormFooterCalculadora';
import { ResultadoSecao } from './campos/ResultadoSecao';
import { SegmentedControl, Colapsavel } from '../Shared/ui';
import { useResultadoFoco } from '../../hooks/useResultadoFoco';
import { useErrosFormulario } from '../../hooks/useErrosFormulario';
import { yAxisProps, formatCurrencyCompacta } from '../Shared/charts/chartTheme';
import { useIsMobile } from '../../hooks/useIsMobile';
import { AvisosFinanciamento } from './AvisosFinanciamento';
import './CalculadoraFinanciamento.css';

type SistemaVisivel = 'price' | 'sac';
type EntradaModo = 'reais' | 'percentual';

/** Chave de UI do modo de amortização — o SegmentedControl só trabalha com string. */
type ModoExtraUi = 'prazo' | 'parcela';
const MODO_EXTRA_API: Record<ModoExtraUi, ModoAmortizacaoExtra> = {
    prazo: ModoAmortizacaoExtra.ReduzirPrazo,
    parcela: ModoAmortizacaoExtra.ReduzirParcela,
};
type CampoErro = 'valor' | 'taxa' | 'prazo' | 'entrada' | 'amortizacoesExtras';
const ID_POR_CAMPO: Record<CampoErro, string> = {
    valor: 'finValor',
    taxa: 'finTaxa-taxa-valor',
    prazo: 'finPrazo',
    entrada: 'finEntrada',
    amortizacoesExtras: 'finAmortizacoesExtras',
};

const PARCELAS_INICIAIS_VISIVEIS = 60;

let proximoIdAmortizacao = 0;
interface AmortizacaoExtraForm {
    id: string;
    mes: string;
    valor: string;
}

/**
 * Converte uma taxa anual (%) na taxa mensal equivalente (%) por juros
 * compostos: i_mensal = (1 + i_anual)^(1/12) - 1. Usado só para traduzir o
 * que o usuário digita para o formato que o backend espera (taxa mensal,
 * como nos contratos de financiamento reais).
 */
const taxaAnualParaMensal = (taxaAnualPercentual: number): number =>
    (Math.pow(1 + taxaAnualPercentual / 100, 1 / 12) - 1) * 100;

/** Indexa as parcelas pelo número para casar os dois cronogramas mês a mês. */
const indexarPorNumero = (parcelas: ParcelaFinanciamentoDto[]) =>
    new Map(parcelas.map(p => [p.numero, p]));

/**
 * Quantos meses o resultado cobre. Price e SAC podem quitar em meses
 * diferentes quando há amortização extra encurtando o prazo, então nada aqui
 * pode assumir que os dois cronogramas têm o mesmo comprimento.
 */
const mesesCobertos = (resultado: FinanciamentoResponseDto) =>
    Math.max(resultado.price.parcelas.length, resultado.sac.parcelas.length);

/** Gera e baixa um CSV com o cronograma de amortização dos dois sistemas lado a lado. */
function exportarCronogramaCsv(resultado: FinanciamentoResponseDto) {
    const colunasPorSistema = (sistema: string) => [
        `${sistema} - Valor`, `${sistema} - Juros`, `${sistema} - Amortizacao`, `${sistema} - Amortizacao Extra`,
        `${sistema} - Seguros`, `${sistema} - Taxa Adm`, `${sistema} - Boleto`, `${sistema} - Saldo Devedor`,
    ];
    const cabecalho = ['Parcela', ...colunasPorSistema('Price'), ...colunasPorSistema('SAC')];

    const price = indexarPorNumero(resultado.price.parcelas);
    const sac = indexarPorNumero(resultado.sac.parcelas);

    // Célula vazia para o sistema que já quitou: casar as duas tabelas por
    // posição quebraria assim que os cronogramas tivessem tamanhos diferentes.
    const celulas = (p?: ParcelaFinanciamentoDto) => p
        ? [
            p.valorParcela.toFixed(2),
            p.juros.toFixed(2),
            p.amortizacao.toFixed(2),
            p.amortizacaoExtra.toFixed(2),
            (p.seguroMip + p.seguroDfi).toFixed(2),
            p.taxaAdministracao.toFixed(2),
            p.parcelaTotal.toFixed(2),
            p.saldoDevedor.toFixed(2),
        ]
        : ['', '', '', '', '', '', '', ''];

    const linhas = Array.from({ length: mesesCobertos(resultado) }, (_, idx) => {
        const numero = idx + 1;
        return [numero, ...celulas(price.get(numero)), ...celulas(sac.get(numero))].join(';');
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

/**
 * Fonte única dos campos do comparativo Price x SAC — consumida pela tabela
 * (desktop, uma linha por sistema) e pelos cartões (celular, um cartão por
 * sistema). Mesmo raciocínio do `linhasComparacao` de `ComparadorCenarios`:
 * evita reescrever os campos duas vezes e correr o risco de as duas versões
 * divergirem numa correção futura. As linhas de prazo e economia só aparecem
 * quando há amortização extra — sem ela seriam sempre iguais e zeradas.
 */
const camposComparativoFinanciamento = (
    temAmortizacaoExtra: boolean,
    temEncargos: boolean,
    temRenda: boolean,
): { chave: string; rotulo: string; valor: (r: ResultadoFinanciamentoDto) => string }[] => [
    { chave: 'parcela1', rotulo: '1ª parcela', valor: r => formatCurrency(r.primeiraParcela) },
    ...(temRenda
        ? [{ chave: 'comprometimento', rotulo: '% da renda', valor: (r: ResultadoFinanciamentoDto) => `${r.comprometimentoRendaPercentual.toFixed(1)}%` }]
        : []),
    ...(temEncargos
        ? [{ chave: 'parcela1Total', rotulo: '1º boleto (com encargos)', valor: (r: ResultadoFinanciamentoDto) => formatCurrency(r.primeiraParcelaTotal) }]
        : []),
    { chave: 'parcelaUltima', rotulo: 'Última parcela', valor: r => formatCurrency(r.ultimaParcela) },
    { chave: 'totalPago', rotulo: 'Total pago', valor: r => formatCurrency(r.totalPago) },
    { chave: 'totalJuros', rotulo: 'Total de juros', valor: r => formatCurrency(r.totalJuros) },
    ...(temEncargos
        ? [
            { chave: 'totalSeguros', rotulo: 'Seguros + tarifas', valor: (r: ResultadoFinanciamentoDto) => formatCurrency(r.totalSeguros + r.totalTaxaAdministracao) },
            { chave: 'totalDesembolsado', rotulo: 'Desembolso total', valor: (r: ResultadoFinanciamentoDto) => formatCurrency(r.totalDesembolsado) },
        ]
        : []),
    { chave: 'jurosFinanciado', rotulo: 'Juros / financiado', valor: r => `${r.jurosSobreFinanciadoPercentual.toFixed(2)}%` },
    { chave: 'cet', rotulo: 'CET (a.a.)', valor: r => r.cetConvergiu ? `${r.cetAnualPercentual.toFixed(2)}%` : '—' },
    ...(temAmortizacaoExtra
        ? [
            { chave: 'prazoFinal', rotulo: 'Prazo final', valor: (r: ResultadoFinanciamentoDto) => formatPrazo(r.prazoFinalMeses) },
            { chave: 'economiaJuros', rotulo: 'Economia de juros', valor: (r: ResultadoFinanciamentoDto) => formatCurrency(r.economiaJuros) },
        ]
        : []),
];

export function CalculadoraFinanciamento() {
    const ehMobile = useIsMobile();
    const [valorImovel, setValorImovel] = useState('');
    const [entradaModo, setEntradaModo] = useState<EntradaModo>('reais');
    const [entrada, setEntrada] = useState('');
    const [periodicidade, setPeriodicidade] = useState<PeriodicidadeTaxa>('mensal');
    const [taxaValor, setTaxaValor] = useState('');
    const [prazo, setPrazo] = useState<PrazoValue>({ valor: '48', unidade: 'meses' });

    const [extraMensal, setExtraMensal] = useState('');
    const [amortizacoesExtras, setAmortizacoesExtras] = useState<AmortizacaoExtraForm[]>([]);
    const [modoExtra, setModoExtra] = useState<ModoExtraUi>('prazo');

    const [taxaInvestimento, setTaxaInvestimento] = useState<TaxaRendimentoValue>({
        modo: 'selic', taxaManual: '', percentualCdi: '',
    });
    const [tipoAtivoInvestimento, setTipoAtivoInvestimento] = useState<TipoAtivoCalculadora>(TipoAtivoCalculadora.TesouroSelic);
    const [isLoadingComparacao, setIsLoadingComparacao] = useState(false);
    const [erroComparacao, setErroComparacao] = useState<string | null>(null);
    const [resultadoComparacao, setResultadoComparacao] = useState<AmortizarVsInvestirResponseDto | null>(null);

    const [seguroMip, setSeguroMip] = useState('');
    const [seguroDfi, setSeguroDfi] = useState('');
    const [taxaAdministracao, setTaxaAdministracao] = useState('');
    const [tarifasContratacao, setTarifasContratacao] = useState('');

    const [itbi, setItbi] = useState('');
    const [custosCartorio, setCustosCartorio] = useState('');
    const [rendaMensal, setRendaMensal] = useState('');

    const [minhaCasaMinhaVida, setMinhaCasaMinhaVida] = useState(false);
    const [subsidioInformado, setSubsidioInformado] = useState('');

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

    const adicionarAmortizacaoExtra = () =>
        setAmortizacoesExtras(prev => [...prev, { id: `amort-${proximoIdAmortizacao++}`, mes: '', valor: '' }]);
    const removerAmortizacaoExtra = (id: string) =>
        setAmortizacoesExtras(prev => prev.filter(a => a.id !== id));
    const atualizarAmortizacaoExtra = (id: string, patch: Partial<AmortizacaoExtraForm>) =>
        setAmortizacoesExtras(prev => prev.map(a => (a.id === id ? { ...a, ...patch } : a)));

    const prazoEmMeses = prazoParaMeses(prazo);
    const opcoesAmortizacaoAtivas =
        (parseCurrency(extraMensal) > 0 ? 1 : 0) + (amortizacoesExtras.length > 0 ? 1 : 0);

    // Prévia do que será financiado, para o usuário conferir a conta antes de simular.
    const financiadoPrevisto = useMemo(() => {
        const imovel = parseCurrency(valorImovel);
        if (!imovel) return null;
        const valorEntrada = entradaModo === 'reais'
            ? parseCurrency(entrada)
            : imovel * (parsePercent(entrada) ?? 0) / 100;
        if (valorEntrada <= 0 || valorEntrada >= imovel) return null;
        return imovel - valorEntrada;
    }, [valorImovel, entrada, entradaModo]);

    const temAmortizacaoExtra = (resultado?.sac.totalAmortizacaoExtra ?? 0) > 0;
    const temEncargos = ((resultado?.sac.totalSeguros ?? 0) + (resultado?.sac.totalTaxaAdministracao ?? 0)) > 0;
    const temCustoAquisicao = ((resultado?.composicao.itbi ?? 0) + (resultado?.composicao.custosCartorio ?? 0)) > 0;

    const encargosAtivos =
        ((parsePercent(seguroMip) ?? 0) > 0 ? 1 : 0) +
        ((parsePercent(seguroDfi) ?? 0) > 0 ? 1 : 0) +
        (parseCurrency(taxaAdministracao) > 0 ? 1 : 0) +
        (parseCurrency(tarifasContratacao) > 0 ? 1 : 0);

    const custoAquisicaoAtivo =
        (parseCurrency(itbi) > 0 ? 1 : 0) +
        (parseCurrency(custosCartorio) > 0 ? 1 : 0) +
        (parseCurrency(rendaMensal) > 0 ? 1 : 0);

    const dadosGrafico = useMemo(() => {
        if (!resultado) return [];
        const price = indexarPorNumero(resultado.price.parcelas);
        const sac = indexarPorNumero(resultado.sac.parcelas);

        return Array.from({ length: mesesCobertos(resultado) }, (_, idx) => {
            const numero = idx + 1;
            // `undefined` nos meses em que aquele sistema já quitou — o Recharts
            // simplesmente interrompe a linha ali, que é a leitura correta
            // ("o Price acabou no mês 213"), em vez de desenhar um zero falso.
            return {
                numero,
                saldoPrice: price.get(numero)?.saldoDevedor,
                saldoSac: sac.get(numero)?.saldoDevedor,
            };
        });
    }, [resultado]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        limparTudo();
        setMostrarTodasParcelas(false);

        const novosErros: Partial<Record<CampoErro, string>> = {};

        const imovel = parseCurrency(valorImovel);
        if (!imovel || imovel <= 0) novosErros.valor = 'Informe o valor do imóvel — precisa ser maior que zero.';

        const valorEntrada = entradaModo === 'reais'
            ? parseCurrency(entrada)
            : imovel * (parsePercent(entrada) ?? 0) / 100;
        if (valorEntrada < 0) {
            novosErros.entrada = 'A entrada não pode ser negativa.';
        } else if (imovel > 0 && valorEntrada >= imovel) {
            novosErros.entrada = 'A entrada precisa ser menor que o valor do imóvel — senão não sobra nada a financiar.';
        }

        // Fora do MCMV, ou sem renda informada, a taxa continua obrigatória:
        // sem faixa resolvida não há teto para usar no lugar dela.
        const taxaEmBrancoPermitida = minhaCasaMinhaVida && parseCurrency(rendaMensal) > 0 && taxaValor.trim() === '';
        const taxaDigitada = parsePercent(taxaValor);
        if (!taxaEmBrancoPermitida && (taxaDigitada === null || taxaDigitada < 0)) {
            novosErros.taxa = 'Informe uma taxa de juros válida.';
        }

        const numParcelas = prazoEmMeses;
        if (!numParcelas || numParcelas <= 0) novosErros.prazo = 'Informe um número de parcelas válido maior que zero.';

        const avulsasValidadas: AmortizacaoExtraAvulsaDto[] = [];
        for (const item of amortizacoesExtras) {
            const mes = parseInt(item.mes, 10);
            const valor = parseCurrency(item.valor);
            if (!mes || mes <= 0 || (numParcelas > 0 && mes > numParcelas) || !valor || valor <= 0) {
                novosErros.amortizacoesExtras =
                    'Confira as amortizações extras: o mês deve estar dentro do prazo e o valor ser maior que zero.';
                break;
            }
            avulsasValidadas.push({ mes, valor });
        }

        const valorExtraMensal = parseCurrency(extraMensal);

        if (Object.keys(novosErros).length > 0) {
            definirEFocar(novosErros, ID_POR_CAMPO);
            return;
        }

        const taxaMensal = taxaDigitada === null
            ? undefined
            : (periodicidade === 'anual' ? taxaAnualParaMensal(taxaDigitada) : taxaDigitada);

        setIsLoading(true);
        setResultado(null);
        try {
            const data = await financiamentoService.simular({
                valorFinanciado: imovel - valorEntrada,
                valorImovel: imovel,
                entrada: valorEntrada,
                taxaJurosMensalPercentual: taxaMensal,
                numParcelas,
                minhaCasaMinhaVida: minhaCasaMinhaVida || undefined,
                subsidioInformado: minhaCasaMinhaVida ? (parseCurrency(subsidioInformado) || undefined) : undefined,
                amortizacaoExtraMensal: valorExtraMensal > 0 ? valorExtraMensal : undefined,
                amortizacoesExtrasAvulsas: avulsasValidadas.length > 0 ? avulsasValidadas : undefined,
                modoAmortizacaoExtra: MODO_EXTRA_API[modoExtra],
                seguroMipMensalPercentualSaldo: parsePercent(seguroMip) ?? undefined,
                seguroDfiMensalPercentualImovel: parsePercent(seguroDfi) ?? undefined,
                taxaAdministracaoMensal: parseCurrency(taxaAdministracao) || undefined,
                tarifasContratacao: parseCurrency(tarifasContratacao) || undefined,
                itbi: parseCurrency(itbi) || undefined,
                custosCartorio: parseCurrency(custosCartorio) || undefined,
                rendaMensal: parseCurrency(rendaMensal) || undefined,
            });
            setResultado(data);
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setErroGeral(axiosError.response?.data?.message || 'Não foi possível simular o financiamento. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    /**
     * Reaproveita os campos já preenchidos do financiamento (imóvel, entrada,
     * taxa, prazo) e do "valor extra todo mês" — não duplica esses campos aqui,
     * só pergunta o que falta: a taxa de investimento e o tipo de ativo.
     */
    const handleCompararAmortizarVsInvestir = async () => {
        setErroComparacao(null);
        setResultadoComparacao(null);

        const imovel = parseCurrency(valorImovel);
        const valorEntrada = entradaModo === 'reais'
            ? parseCurrency(entrada)
            : imovel * (parsePercent(entrada) ?? 0) / 100;
        const taxaDigitada = parsePercent(taxaValor);
        const taxaMensal = taxaDigitada === null
            ? null
            : (periodicidade === 'anual' ? taxaAnualParaMensal(taxaDigitada) : taxaDigitada);
        const numParcelas = prazoEmMeses;
        const valorExtraMensal = parseCurrency(extraMensal);

        if (!imovel || imovel <= 0 || valorEntrada >= imovel || taxaMensal === null || !numParcelas || numParcelas <= 0) {
            setErroComparacao('Preencha o valor do imóvel, a entrada, a taxa e o prazo do financiamento antes de comparar.');
            return;
        }
        if (valorExtraMensal <= 0) {
            setErroComparacao('Informe um valor extra mensal maior que zero para comparar.');
            return;
        }
        const erroTaxaInvestimento = validarTaxaRendimento(taxaInvestimento);
        if (erroTaxaInvestimento) {
            setErroComparacao(erroTaxaInvestimento);
            return;
        }

        const { fonteTaxaJuros, taxaJurosAnualPercentual, percentualCdi } = parametrosTaxa(taxaInvestimento);

        setIsLoadingComparacao(true);
        try {
            const data = await financiamentoService.amortizarVsInvestir({
                valorFinanciado: imovel - valorEntrada,
                taxaJurosMensalPercentual: taxaMensal,
                numParcelas,
                sistema: SistemaAmortizacao.Sac,
                valorDisponivelMensal: valorExtraMensal,
                tipoAtivoInvestimento,
                fonteTaxaJurosInvestimento: fonteTaxaJuros,
                taxaJurosAnualInvestimentoPercentual: taxaJurosAnualPercentual,
                percentualCdiInvestimento: percentualCdi,
            });
            setResultadoComparacao(data);
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setErroComparacao(axiosError.response?.data?.message || 'Não foi possível comparar. Tente novamente.');
        } finally {
            setIsLoadingComparacao(false);
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

    const temRenda = ((resultado?.sac.comprometimentoRendaPercentual ?? 0) > 0);
    const camposComparativo = camposComparativoFinanciamento(temAmortizacaoExtra, temEncargos, temRenda);
    const sistemas = resultado
        ? ([
            { sistema: SistemaAmortizacao.Price, dados: resultado.price },
            { sistema: SistemaAmortizacao.Sac, dados: resultado.sac },
        ] as const)
        : [];

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
                        label="Valor do imóvel (R$)"
                        value={valorImovel}
                        onChange={v => { setValorImovel(v); limpar('valor'); }}
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

                <div className="campo-form-group">
                    <label htmlFor="finEntrada">Entrada</label>
                    <div className="fin-entrada-linha">
                        <SegmentedControl
                            value={entradaModo}
                            onChange={v => { setEntradaModo(v); setEntrada(''); limpar('entrada'); }}
                            ariaLabel="Informar a entrada em reais ou em percentual"
                            opcoes={[
                                { valor: 'reais', rotulo: 'R$' },
                                { valor: 'percentual', rotulo: '%' },
                            ]}
                            disabled={isLoading}
                        />
                        <input
                            id="finEntrada"
                            type="text"
                            inputMode={entradaModo === 'reais' ? 'numeric' : 'decimal'}
                            placeholder={entradaModo === 'reais' ? '0,00' : 'Ex: 20'}
                            value={entrada}
                            onChange={e => {
                                setEntrada(entradaModo === 'reais' ? maskCurrency(e.target.value) : e.target.value);
                                limpar('entrada');
                            }}
                            disabled={isLoading}
                            aria-invalid={!!erros.entrada}
                            aria-describedby={erros.entrada ? 'finEntrada-erro' : undefined}
                            className={erros.entrada ? 'campo-input--erro' : undefined}
                        />
                    </div>
                    <p className="campo-hint">
                        {financiadoPrevisto !== null
                            ? `Valor financiado: ${formatCurrency(financiadoPrevisto)} — a entrada não paga juros.`
                            : 'Opcional. Quanto maior a entrada, menos juros você paga no total.'}
                    </p>
                    {erros.entrada && <span id="finEntrada-erro" className="campo-erro">{erros.entrada}</span>}
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

                <Colapsavel
                    titulo="Amortização extra — pagar a mais para quitar antes"
                    selo={opcoesAmortizacaoAtivas > 0 ? `${opcoesAmortizacaoAtivas} ativa${opcoesAmortizacaoAtivas > 1 ? 's' : ''}` : undefined}
                >
                    <p className="campo-hint">
                        Todo real pago além da parcela abate o saldo devedor direto, sem passar por juros.
                        Você escolhe o que o abatimento faz com o contrato.
                    </p>

                    <div className="campo-form-group">
                        <label htmlFor="finModoExtra">O que a amortização extra deve reduzir</label>
                        <SegmentedControl
                            value={modoExtra}
                            onChange={setModoExtra}
                            ariaLabel="O que a amortização extra deve reduzir"
                            full
                            opcoes={[
                                { valor: 'prazo', rotulo: 'Reduzir prazo' },
                                { valor: 'parcela', rotulo: 'Reduzir parcela' },
                            ]}
                            disabled={isLoading}
                        />
                        <p className="campo-hint">
                            {modoExtra === 'prazo'
                                ? 'Quita as últimas parcelas e encerra o contrato antes. É o que mais economiza juros.'
                                : 'Mantém o prazo e recalcula a parcela sobre o saldo menor. Alivia o mês a mês, economiza menos.'}
                        </p>
                    </div>

                    <CampoMoeda
                        id="finExtraMensal"
                        label="Valor extra todo mês (R$)"
                        value={extraMensal}
                        onChange={setExtraMensal}
                        disabled={isLoading}
                        hint="Somado a toda parcela, junto da mensalidade."
                    />

                    <div className="campo-form-group" id="finAmortizacoesExtras">
                        <label>Amortizações pontuais (FGTS, 13º)</label>
                        {amortizacoesExtras.map(item => (
                            <div key={item.id} className="proj-aporte-extra-row">
                                <input
                                    type="number"
                                    min={1}
                                    max={prazoEmMeses || undefined}
                                    placeholder="Mês"
                                    value={item.mes}
                                    onChange={e => { atualizarAmortizacaoExtra(item.id, { mes: e.target.value }); limpar('amortizacoesExtras'); }}
                                    disabled={isLoading}
                                />
                                <input
                                    type="text"
                                    inputMode="numeric"
                                    placeholder="Valor (R$)"
                                    value={item.valor}
                                    onChange={e => { atualizarAmortizacaoExtra(item.id, { valor: maskCurrency(e.target.value) }); limpar('amortizacoesExtras'); }}
                                    disabled={isLoading}
                                />
                                <button
                                    type="button"
                                    className="proj-aporte-extra-remover"
                                    onClick={() => removerAmortizacaoExtra(item.id)}
                                    disabled={isLoading}
                                    aria-label={`Remover amortização do mês ${item.mes || 'não informado'}`}
                                >
                                    ✕
                                </button>
                            </div>
                        ))}
                        <button
                            type="button"
                            className="proj-aporte-extra-adicionar"
                            onClick={adicionarAmortizacaoExtra}
                            disabled={isLoading}
                        >
                            + Adicionar amortização pontual
                        </button>
                        {erros.amortizacoesExtras && <span className="campo-erro">{erros.amortizacoesExtras}</span>}
                    </div>

                    {parseCurrency(extraMensal) > 0 && (
                        <div className="fin-vs-investir">
                            <p className="fin-vs-investir-titulo">
                                Vale mais a pena amortizar esse extra ou investir esse dinheiro?
                            </p>
                            <p className="campo-hint">
                                Compara os {formatCurrency(parseCurrency(extraMensal))} de extra mensal quitando o
                                financiamento antes contra o mesmo valor investido pelo mesmo prazo, líquido de
                                imposto. Usa o valor do imóvel, a entrada, a taxa e o prazo já preenchidos acima.
                            </p>

                            <CampoTaxaRendimento
                                idPrefix="finVsInvestir"
                                label="Taxa de retorno do investimento"
                                value={taxaInvestimento}
                                onChange={setTaxaInvestimento}
                                disabled={isLoadingComparacao}
                            />
                            <CampoTipoAtivo
                                id="finVsInvestirTipoAtivo"
                                value={tipoAtivoInvestimento}
                                onChange={setTipoAtivoInvestimento}
                                disabled={isLoadingComparacao}
                            />

                            <button
                                type="button"
                                className="campo-btn-submit campo-btn-submit--secundaria"
                                onClick={handleCompararAmortizarVsInvestir}
                                disabled={isLoadingComparacao}
                            >
                                {isLoadingComparacao ? 'Comparando…' : 'Comparar amortizar vs. investir'}
                            </button>

                            {erroComparacao && (
                                <p className="campo-erro" role="alert">{erroComparacao}</p>
                            )}

                            {resultadoComparacao && (
                                <div className={`fin-vs-investir-resultado fin-vs-investir-resultado--${
                                    resultadoComparacao.recomendacao === RecomendacaoFinanceira.Investir ? 'investir' : 'amortizar'
                                }`}>
                                    <p className="fin-vs-investir-recomendacao">
                                        {resultadoComparacao.recomendacao === RecomendacaoFinanceira.Investir ? '💰 ' : '🏠 '}
                                        {ROTULO_RECOMENDACAO[resultadoComparacao.recomendacao]}
                                    </p>
                                    <ul className="fin-vs-investir-detalhes">
                                        <li>Juros economizados amortizando: <strong>{formatCurrency(resultadoComparacao.economiaJurosAmortizando)}</strong></li>
                                        <li>Valor final líquido investindo: <strong>{formatCurrency(resultadoComparacao.valorFinalLiquidoInvestindo)}</strong></li>
                                        <li>Diferença a favor de {resultadoComparacao.recomendacao === RecomendacaoFinanceira.Investir ? 'investir' : 'amortizar'}: <strong>{formatCurrency(Math.abs(resultadoComparacao.diferenca))}</strong></li>
                                    </ul>
                                </div>
                            )}
                        </div>
                    )}
                </Colapsavel>

                <Colapsavel
                    titulo="Seguros e taxas obrigatórios"
                    selo={encargosAtivos > 0 ? `${encargosAtivos} ativo${encargosAtivos > 1 ? 's' : ''}` : undefined}
                >
                    <p className="campo-hint">
                        Todo financiamento imobiliário cobra seguro de morte e invalidez (MIP), seguro de danos
                        ao imóvel (DFI) e uma tarifa mensal de administração. Eles não abatem a dívida, mas
                        entram no boleto — sem informá-los, a parcela simulada fica abaixo da que o banco cobra.
                        Os percentuais estão na proposta do banco.
                    </p>

                    <div className="proj-form-row">
                        <div className="campo-form-group">
                            <label htmlFor="finSeguroMip">Seguro MIP (% a.m. sobre o saldo devedor)</label>
                            <input
                                id="finSeguroMip"
                                type="text"
                                inputMode="decimal"
                                placeholder="Ex: 0,025"
                                value={seguroMip}
                                onChange={e => setSeguroMip(e.target.value)}
                                disabled={isLoading}
                            />
                            <p className="campo-hint">Cai junto com a dívida — é maior no começo do contrato.</p>
                        </div>
                        <div className="campo-form-group">
                            <label htmlFor="finSeguroDfi">Seguro DFI (% a.m. sobre o imóvel)</label>
                            <input
                                id="finSeguroDfi"
                                type="text"
                                inputMode="decimal"
                                placeholder="Ex: 0,01"
                                value={seguroDfi}
                                onChange={e => setSeguroDfi(e.target.value)}
                                disabled={isLoading}
                            />
                            <p className="campo-hint">Praticamente fixo: incide sobre o valor do imóvel.</p>
                        </div>
                    </div>

                    <div className="proj-form-row">
                        <CampoMoeda
                            id="finTaxaAdministracao"
                            label="Taxa de administração mensal (R$)"
                            value={taxaAdministracao}
                            onChange={setTaxaAdministracao}
                            disabled={isLoading}
                            hint="Tarifa fixa do contrato — na Caixa costuma ficar em torno de R$ 25."
                        />
                        <CampoMoeda
                            id="finTarifasContratacao"
                            label="Tarifas de contratação (R$)"
                            value={tarifasContratacao}
                            onChange={setTarifasContratacao}
                            disabled={isLoading}
                            hint="Avaliação do imóvel, IOF — descontadas do valor liberado. Usadas só no cálculo do CET."
                        />
                    </div>
                </Colapsavel>

                <Colapsavel
                    titulo="Custo de aquisição e renda"
                    selo={custoAquisicaoAtivo > 0 ? `${custoAquisicaoAtivo} ativo${custoAquisicaoAtivo > 1 ? 's' : ''}` : undefined}
                >
                    <p className="campo-hint">
                        ITBI, escritura e registro não entram no financiamento — são pagos à parte, no fechamento
                        do negócio. Informe sua renda para o simulador avisar se a parcela pesa mais que os 30%
                        que os bancos costumam aceitar.
                    </p>

                    <div className="proj-form-row">
                        <CampoMoeda
                            id="finItbi"
                            label="ITBI (R$)"
                            value={itbi}
                            onChange={setItbi}
                            disabled={isLoading}
                            hint="Imposto de transmissão, pago ao município — geralmente 2 a 3% do imóvel."
                        />
                        <CampoMoeda
                            id="finCustosCartorio"
                            label="Escritura + registro (R$)"
                            value={custosCartorio}
                            onChange={setCustosCartorio}
                            disabled={isLoading}
                        />
                    </div>

                    <CampoMoeda
                        id="finRendaMensal"
                        label="Renda familiar bruta mensal (R$)"
                        value={rendaMensal}
                        onChange={setRendaMensal}
                        disabled={isLoading}
                        hint="Usada só para o aviso de comprometimento de renda — não é enviada a nenhum lugar além desta simulação."
                    />
                </Colapsavel>

                <div className="fin-mcmv-toggle">
                    <label className="fin-mcmv-toggle-label" htmlFor="finMcmv">
                        <input
                            id="finMcmv"
                            type="checkbox"
                            checked={minhaCasaMinhaVida}
                            onChange={e => setMinhaCasaMinhaVida(e.target.checked)}
                            disabled={isLoading}
                        />
                        Este financiamento é pelo Minha Casa Minha Vida
                    </label>
                    {minhaCasaMinhaVida && (
                        <div className="fin-mcmv-conteudo">
                            <p className="campo-hint">
                                A faixa é resolvida automaticamente pela renda informada em "Custo de aquisição e
                                renda" acima. Deixe a taxa de juros do contrato em branco para usar o teto da faixa —
                                a leitura conservadora, que nunca promete uma parcela menor do que a pior taxa
                                possível nela.
                            </p>
                            <CampoMoeda
                                id="finSubsidio"
                                label="Subsídio oferecido pela Caixa (R$)"
                                value={subsidioInformado}
                                onChange={setSubsidioInformado}
                                disabled={isLoading}
                                hint="É 'até' — nunca calculado automaticamente. Digite o que a Caixa ofereceu na sua simulação."
                            />
                        </div>
                    )}
                </div>

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
                    <AvisosFinanciamento avisos={resultado.avisos} />

                    {resultado.faixaMcmv && (
                        <div className="fin-mcmv-faixa">
                            <p className="fin-mcmv-faixa-titulo">
                                {ROTULO_FAIXA_MCMV[resultado.faixaMcmv.faixa] ?? resultado.faixaMcmv.faixa} do Minha Casa Minha Vida
                            </p>
                            <ul className="fin-mcmv-faixa-detalhes">
                                <li>Renda até <strong>{formatCurrency(resultado.faixaMcmv.rendaMaxima)}</strong></li>
                                <li>Taxa da faixa <strong>{resultado.faixaMcmv.taxaAnualMinimaPercentual.toFixed(2)}% a {resultado.faixaMcmv.taxaAnualMaximaPercentual.toFixed(2)}% a.a.</strong></li>
                                <li>Teto de imóvel (referência) <strong>{formatCurrency(resultado.faixaMcmv.tetoImovelReferencia)}</strong></li>
                                <li>Entrada mínima <strong>{resultado.faixaMcmv.entradaMinimaPercentual.toFixed(0)}%</strong></li>
                            </ul>
                        </div>
                    )}
                    {resultado.vigenciaReferenciaMcmv && (
                        <p className="fin-mcmv-vigencia">
                            Valores de referência do Minha Casa Minha Vida — vigência {resultado.vigenciaReferenciaMcmv}.
                            Tetos, faixas e subsídios variam por região e mudam por decreto; confirme sempre com a Caixa.
                        </p>
                    )}

                    <div className="proj-result-stats">
                        <div className="proj-result-stat proj-result-stat--highlight">
                            <span className="proj-result-stat-value proj-result-stat-value--texto">
                                {ROTULO_SISTEMA[resultado.sistemaMaisBarato]}
                            </span>
                            <span className="proj-result-stat-label">Sistema com menos juros totais</span>
                        </div>
                        <div className="proj-result-stat">
                            <span className="proj-result-stat-value proj-result-stat-value--green">
                                {formatCurrency(resultado.diferencaTotalJuros)}
                            </span>
                            <span className="proj-result-stat-label">Economia escolhendo o sistema mais barato</span>
                        </div>
                        <div className="proj-result-stat">
                            <span className="proj-result-stat-value">{formatCurrency(resultado.composicao.valorFinanciado)}</span>
                            <span className="proj-result-stat-label">
                                Valor financiado
                                {resultado.composicao.entrada > 0 && ` (entrada de ${resultado.composicao.entradaPercentual.toFixed(1)}%)`}
                            </span>
                        </div>
                        <div className="proj-result-stat">
                            <span className="proj-result-stat-value">{formatCurrency(resultado.sac.totalJuros)}</span>
                            <span className="proj-result-stat-label">Total de juros — SAC</span>
                        </div>
                    </div>

                    {temAmortizacaoExtra && (
                        <div className="proj-result-stats">
                            <div className="proj-result-stat">
                                <span className="proj-result-stat-value proj-result-stat-value--green">
                                    {formatCurrency(resultado.sac.economiaJuros)}
                                </span>
                                <span className="proj-result-stat-label">Juros economizados no SAC</span>
                            </div>
                            <div className="proj-result-stat">
                                <span className="proj-result-stat-value proj-result-stat-value--texto">
                                    {resultado.sac.mesesEconomizados > 0
                                        ? formatPrazo(resultado.sac.mesesEconomizados)
                                        : 'prazo mantido'}
                                </span>
                                <span className="proj-result-stat-label">
                                    {resultado.sac.mesesEconomizados > 0 ? 'Quitação antecipada no SAC' : 'A parcela caiu no lugar do prazo'}
                                </span>
                            </div>
                            <div className="proj-result-stat">
                                <span className="proj-result-stat-value">{formatCurrency(resultado.sac.totalAmortizacaoExtra)}</span>
                                <span className="proj-result-stat-label">Total pago em amortização extra</span>
                            </div>
                        </div>
                    )}

                    {temEncargos && (
                        <div className="proj-result-stats">
                            <div className="proj-result-stat">
                                <span className="proj-result-stat-value">{formatCurrency(resultado.sac.primeiraParcelaTotal)}</span>
                                <span className="proj-result-stat-label">1º boleto no SAC (com seguros e tarifa)</span>
                            </div>
                            <div className="proj-result-stat">
                                <span className="proj-result-stat-value">
                                    {formatCurrency(resultado.sac.totalSeguros + resultado.sac.totalTaxaAdministracao)}
                                </span>
                                <span className="proj-result-stat-label">Seguros + tarifas no SAC</span>
                            </div>
                            <div className="proj-result-stat">
                                <span className="proj-result-stat-value">{formatCurrency(resultado.sac.totalDesembolsado)}</span>
                                <span className="proj-result-stat-label">Desembolso total no SAC</span>
                            </div>
                        </div>
                    )}

                    {temCustoAquisicao && (
                        <div className="proj-result-stats">
                            <div className="proj-result-stat">
                                <span className="proj-result-stat-value">{formatCurrency(resultado.composicao.itbi + resultado.composicao.custosCartorio)}</span>
                                <span className="proj-result-stat-label">ITBI + escritura + registro</span>
                            </div>
                            <div className="proj-result-stat">
                                <span className="proj-result-stat-value">{formatCurrency(resultado.composicao.desembolsoInicial)}</span>
                                <span className="proj-result-stat-label">Desembolso inicial (entrada + custos)</span>
                            </div>
                        </div>
                    )}

                    <p className="calc-resultado-eco">
                        O sistema <strong>{ROTULO_SISTEMA[resultado.sistemaMaisBarato]}</strong> sai {formatCurrency(resultado.diferencaTotalJuros)} mais barato no total.
                        {temEncargos && ' A comparação é pelos juros; os seguros e tarifas entram no desembolso total.'}
                    </p>
                    <p className="campo-hint">
                        CET (Custo Efetivo Total) é a taxa que já embute seguros e tarifas de contratação —
                        o número certo para comparar com a proposta de outro banco. Não inclui entrada, ITBI
                        nem cartório: isso não é custo do empréstimo, é custo do negócio do imóvel.
                    </p>

                    {/* Desktop: tabela — só 2 linhas (Price, SAC), cabe sem apertar. */}
                    <div className="tabela-wrap">
                        <table className="tabela tabela--numerica fin-tabela--comparativo">
                            <thead>
                                <tr>
                                    <th>Sistema</th>
                                    {camposComparativo.map(c => <th key={c.chave}>{c.rotulo}</th>)}
                                </tr>
                            </thead>
                            <tbody>
                                {sistemas.map(({ sistema, dados }) => (
                                    <tr key={sistema} className={resultado.sistemaMaisBarato === sistema ? 'tabela-row--destaque' : undefined}>
                                        <td>{ROTULO_SISTEMA[sistema]}</td>
                                        {camposComparativo.map(c => <td key={c.chave}>{c.valor(dados)}</td>)}
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    {/* Celular: um cartão por sistema, mesma fonte de campos da tabela. */}
                    <ul className="fin-cartoes">
                        {sistemas.map(({ sistema, dados }) => (
                            <li
                                key={sistema}
                                className={`fin-cartao${resultado.sistemaMaisBarato === sistema ? ' fin-cartao--melhor' : ''}`}
                            >
                                <div className="fin-cartao-cabecalho">
                                    <span className="fin-cartao-titulo">{ROTULO_SISTEMA[sistema]}</span>
                                    {resultado.sistemaMaisBarato === sistema && <span className="tabela-badge">mais barato</span>}
                                </div>
                                <dl className="fin-cartao-lista">
                                    {camposComparativo.map(c => (
                                        <div key={c.chave} className="fin-cartao-linha">
                                            <dt>{c.rotulo}</dt>
                                            <dd>{c.valor(dados)}</dd>
                                        </div>
                                    ))}
                                </dl>
                            </li>
                        ))}
                    </ul>

                    <div className="proj-chart">
                        <ResponsiveContainer width="100%" height={ehMobile ? 220 : 280}>
                            <ComposedChart data={dadosGrafico} margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
                                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                                <XAxis dataKey="numero" stroke="#94a3b8" fontSize={12} />
                                <YAxis tickFormatter={(v: number) => ehMobile ? formatCurrencyCompacta(v) : formatCurrency(v)} {...yAxisProps(ehMobile)} />
                                <Tooltip
                                    formatter={(value, name) => [value == null ? 'quitado' : formatCurrency(Number(value)), name]}
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
                            rolavel
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
                        <div className="tabela-wrap tabela-wrap--scroll">
                            <table className="tabela tabela--numerica">
                                <thead>
                                    <tr>
                                        <th>Nº</th>
                                        <th>Parcela</th>
                                        <th>Juros</th>
                                        <th>Amortização</th>
                                        {temAmortizacaoExtra && <th>Extra</th>}
                                        {temEncargos && <th>Seguros</th>}
                                        {temEncargos && <th>Taxa adm.</th>}
                                        {temEncargos && <th>Boleto</th>}
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
                                            {temAmortizacaoExtra && <td>{formatCurrency(p.amortizacaoExtra)}</td>}
                                            {temEncargos && <td>{formatCurrency(p.seguroMip + p.seguroDfi)}</td>}
                                            {temEncargos && <td>{formatCurrency(p.taxaAdministracao)}</td>}
                                            {temEncargos && <td>{formatCurrency(p.parcelaTotal)}</td>}
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
