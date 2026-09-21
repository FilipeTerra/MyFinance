import type { ModoAmortizacaoExtra, SistemaAmortizacao } from './SistemaAmortizacao';

export interface AmortizacaoExtraAvulsaDto {
    mes: number;
    valor: number;
}

export interface FinanciamentoRequestDto {
    /** Ignorado quando `valorImovel` é informado — aí o financiado é derivado. */
    valorFinanciado: number;
    taxaJurosMensalPercentual: number;
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
}

export interface ComposicaoFinanciamentoDto {
    valorImovel: number;
    entrada: number;
    entradaPercentual: number;
    valorFinanciado: number;
}

export interface FinanciamentoResponseDto {
    price: ResultadoFinanciamentoDto;
    sac: ResultadoFinanciamentoDto;
    /** Qual dos dois sistemas resulta em menos juros pagos. */
    sistemaMaisBarato: SistemaAmortizacao;
    diferencaTotalJuros: number;
    composicao: ComposicaoFinanciamentoDto;
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
