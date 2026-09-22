import type { ModoAmortizacaoExtra, SistemaAmortizacao, RecomendacaoFinanceira } from './SistemaAmortizacao';
import type { FonteTaxaJuros } from './FonteTaxaJuros';
import type { TipoAtivoCalculadora } from './TipoAtivoCalculadora';
import type { ProjecaoInvestimentoResponseDto } from './ProjecaoInvestimento';

export interface AmortizacaoExtraAvulsaDto {
    mes: number;
    valor: number;
}

export interface FinanciamentoRequestDto {
    /** Ignorado quando `valorImovel` é informado — aí o financiado é derivado. */
    valorFinanciado: number;
    /** Opcional só quando `minhaCasaMinhaVida` é true e a renda resolve uma faixa — aí usa-se o teto dela. */
    taxaJurosMensalPercentual?: number;
    numParcelas: number;
    /** Preço do imóvel. Quando informado, vira a base e o financiado é o que sobra após a entrada. */
    valorImovel?: number;
    /** Entrada em R$. Tem precedência sobre `entradaPercentual`. */
    entrada?: number;
    entradaPercentual?: number;
    /** Valor pago a mais em toda parcela, abatido direto do principal. */
    amortizacaoExtraMensal?: number;
    /** Pagamentos extras pontuais (FGTS, 13º). */
    amortizacoesExtrasAvulsas?: AmortizacaoExtraAvulsaDto[];
    modoAmortizacaoExtra?: ModoAmortizacaoExtra;
    /** Alíquota mensal do seguro MIP sobre o saldo devedor, em % (ex.: 0.025). */
    seguroMipMensalPercentualSaldo?: number;
    /** Alíquota mensal do seguro DFI sobre o valor do imóvel, em % (ex.: 0.01). */
    seguroDfiMensalPercentualImovel?: number;
    /** Tarifa mensal de administração, em R$. */
    taxaAdministracaoMensal?: number;
    /** Tarifas cobradas pelo credor na contratação, descontadas do valor liberado. */
    tarifasContratacao?: number;
    /** ITBI, em R$. Pago ao município — não afeta o valor financiado. */
    itbi?: number;
    /** Escritura + registro, em R$. */
    custosCartorio?: number;
    /** Renda familiar bruta mensal — usada só para avisar se a parcela pesa mais de 30% dela. */
    rendaMensal?: number;
    /** Liga as regras do Minha Casa Minha Vida (faixa, subsídio, avisos do programa). Padrão: desligado. */
    minhaCasaMinhaVida?: boolean;
    /** Subsídio que a Caixa ofereceu, em R$. Só usado quando `minhaCasaMinhaVida` é true. */
    subsidioInformado?: number;
}

export interface ParcelaFinanciamentoDto {
    numero: number;
    /** Parcela contratual do mês, sem o pagamento extra. */
    valorParcela: number;
    juros: number;
    amortizacao: number;
    saldoDevedor: number;
    amortizacaoExtra: number;
    /** Seguro de morte e invalidez do mês, sobre o saldo devedor. */
    seguroMip: number;
    /** Seguro de danos físicos ao imóvel do mês. */
    seguroDfi: number;
    taxaAdministracao: number;
    /** Desembolso real do mês: parcela contratual + extra + seguros + tarifa. */
    parcelaTotal: number;
}

export interface ResultadoFinanciamentoDto {
    primeiraParcela: number;
    ultimaParcela: number;
    totalPago: number;
    totalJuros: number;
    /** Juros totais sobre o valor financiado (%). **Não é o CET.** */
    jurosSobreFinanciadoPercentual: number;
    parcelas: ParcelaFinanciamentoDto[];
    /** Em quantos meses o contrato quitou de fato. */
    prazoFinalMeses: number;
    totalAmortizacaoExtra: number;
    /** Juros economizados em relação ao mesmo contrato sem amortização extra. */
    economiaJuros: number;
    mesesEconomizados: number;
    /** Total pago em seguros obrigatórios (MIP + DFI) ao longo do contrato. */
    totalSeguros: number;
    totalTaxaAdministracao: number;
    /** Desembolso do 1º mês já com extras, seguros e tarifa — o valor do boleto. */
    primeiraParcelaTotal: number;
    /** Tudo que sai do bolso: parcelas, extras, seguros e tarifas. */
    totalDesembolsado: number;
    /** Custo Efetivo Total anual — inclui seguros, tarifas e o efeito do tempo. */
    cetAnualPercentual: number;
    cetMensalPercentual: number;
    /** Falso quando a TIR não convergiu. Exibir "—", nunca 0%. */
    cetConvergiu: boolean;
    /** Quanto o 1º boleto representa da renda informada. Zero quando a renda não foi informada. */
    comprometimentoRendaPercentual: number;
}

export interface ComposicaoFinanciamentoDto {
    valorImovel: number;
    entrada: number;
    entradaPercentual: number;
    valorFinanciado: number;
    itbi: number;
    custosCartorio: number;
    /** Entrada + ITBI + cartório — quanto precisa estar em caixa além do financiamento. */
    desembolsoInicial: number;
    /** Subsídio do MCMV, em R$. Abate o financiado, mas não é pago pelo comprador. */
    subsidio: number;
}

export interface FaixaMcmvDto {
    /** "Faixa1" a "Faixa4" — nome do enum, não texto pronto para exibir. */
    faixa: string;
    rendaMaxima: number;
    taxaAnualMinimaPercentual: number;
    taxaAnualMaximaPercentual: number;
    tetoImovelReferencia: number;
    entradaMinimaPercentual: number;
    subsidioMaximoPercentualImovel: number | null;
    subsidioMaximoValor: number | null;
}

/** Rótulo de UI de cada faixa do MCMV, a partir do nome do enum devolvido pela API. */
export const ROTULO_FAIXA_MCMV: Record<string, string> = {
    Faixa1: 'Faixa 1',
    Faixa2: 'Faixa 2',
    Faixa3: 'Faixa 3',
    Faixa4: 'Faixa 4'
};

export type SeveridadeAvisoFinanciamento = 'Informativo' | 'Atencao' | 'Critico';

export interface AvisoFinanciamentoDto {
    /** Chave estável em inglês — usar para lógica, não a mensagem. */
    codigo: string;
    mensagem: string;
    severidade: SeveridadeAvisoFinanciamento;
}

export interface FinanciamentoResponseDto {
    price: ResultadoFinanciamentoDto;
    sac: ResultadoFinanciamentoDto;
    /** Qual dos dois sistemas resulta em menos juros pagos. */
    sistemaMaisBarato: SistemaAmortizacao;
    diferencaTotalJuros: number;
    composicao: ComposicaoFinanciamentoDto;
    avisos: AvisoFinanciamentoDto[];
    /** Faixa do MCMV resolvida pela renda. Nulo se o MCMV não foi ligado, ou se a renda está fora de todas as faixas. */
    faixaMcmv: FaixaMcmvDto | null;
    /** Mês de referência dos valores do MCMV usados. Presente sempre que `minhaCasaMinhaVida` foi ligado. */
    vigenciaReferenciaMcmv: string | null;
}

export interface TaxaEfetivaRequestDto {
    taxaNominalAnualPercentual: number;
    capitalizacoesPorAno: number;
}

export interface TaxaEfetivaResponseDto {
    taxaNominalAnualPercentual: number;
    capitalizacoesPorAno: number;
    taxaEfetivaAnualPercentual: number;
}

export interface AmortizarVsInvestirRequestDto {
    valorFinanciado: number;
    taxaJurosMensalPercentual: number;
    numParcelas: number;
    sistema: SistemaAmortizacao;
    /** Quanto está disponível por mês — o mesmo valor testado nos dois cenários. */
    valorDisponivelMensal: number;
    fonteTaxaJurosInvestimento: FonteTaxaJuros;
    /** Obrigatória quando `fonteTaxaJurosInvestimento` é Manual. */
    taxaJurosAnualInvestimentoPercentual?: number;
    /** Obrigatório quando `fonteTaxaJurosInvestimento` é PercentualCdi. */
    percentualCdiInvestimento?: number;
    tipoAtivoInvestimento: TipoAtivoCalculadora;
}

export interface AmortizarVsInvestirResponseDto {
    recomendacao: RecomendacaoFinanceira;
    /** Juros que deixariam de ser pagos se o valor virasse amortização extra (prazo reduzido). */
    economiaJurosAmortizando: number;
    /** Valor final líquido (já com IR/IOF) se o mesmo valor fosse investido pelo mesmo prazo. */
    valorFinalLiquidoInvestindo: number;
    /** Investir menos amortizar. Positivo favorece investir; negativo favorece amortizar. */
    diferenca: number;
    projecaoInvestindo: ProjecaoInvestimentoResponseDto;
}
