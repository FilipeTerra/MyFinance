export interface FinanciamentoRequestDto {
    valorFinanciado: number;
    taxaJurosMensalPercentual: number;
    numParcelas: number;
}

export interface ParcelaFinanciamentoDto {
    numero: number;
    valorParcela: number;
    juros: number;
    amortizacao: number;
    saldoDevedor: number;
}

export interface ResultadoFinanciamentoDto {
    primeiraParcela: number;
    ultimaParcela: number;
    totalPago: number;
    totalJuros: number;
    custoEfetivoTotalPercentual: number;
    parcelas: ParcelaFinanciamentoDto[];
}

export interface FinanciamentoResponseDto {
    price: ResultadoFinanciamentoDto;
    sac: ResultadoFinanciamentoDto;
    /** "Price" ou "SAC" — qual dos dois sistemas resulta em menos juros pagos. */
    sistemaMaisBarato: string;
    diferencaTotalJuros: number;
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
