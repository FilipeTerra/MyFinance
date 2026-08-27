import type { TipoAtivoCalculadora } from './TipoAtivoCalculadora';
import type { FonteTaxaJuros } from './FonteTaxaJuros';

interface TaxaConfig {
    fonteTaxaJuros: FonteTaxaJuros;
    taxaJurosAnualPercentual?: number;
    percentualCdi?: number;
    tipoAtivo: TipoAtivoCalculadora;
}

export interface CalcularSaqueSustentavelRequestDto extends TaxaConfig {
    saldoInicial: number;
    /** Parte do saldo inicial que corresponde ao que foi efetivamente aportado (base de custo, não ganho). */
    baseCustoInicial: number;
    prazoMeses: number;
}

export interface CalcularDuracaoRetiradaRequestDto extends TaxaConfig {
    saldoInicial: number;
    baseCustoInicial: number;
    saqueMensal: number;
}

export interface MesRetiradaDto {
    mes: number;
    saldoInicial: number;
    saqueBruto: number;
    aliquotaImpostoPercentual: number;
    valorImposto: number;
    saqueLiquido: number;
    saldoFinal: number;
}

export interface RetiradaResponseDto {
    saqueMensal: number;
    duraParaSempre: boolean;
    mesEsgotamento?: number;
    taxaJurosAnualUtilizada: number;
    percentualCdiUtilizado?: number;
    cdiAnualUtilizado?: number;
    evolucao: MesRetiradaDto[];
}
