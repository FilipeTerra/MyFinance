export interface CalcularProjecaoRequestDto {
    aporteInicial: number;
    aporteMensal: number;
    prazoMeses: number;
    /** Obrigatório quando usarTaxaSelic é falso. */
    taxaJurosAnualPercentual?: number;
    /** Quando verdadeiro, ignora taxaJurosAnualPercentual e usa a Selic real vigente. */
    usarTaxaSelic: boolean;
}

export interface MesProjecaoDto {
    mes: number;
    valorAcumulado: number;
    totalAportadoAcumulado: number;
    jurosAcumulado: number;
}

export interface ProjecaoInvestimentoResponseDto {
    valorFinal: number;
    totalAportado: number;
    totalJuros: number;
    rentabilidadePercentual: number;
    /** Taxa de juros anual (%) efetivamente usada — manual ou Selic real. */
    taxaJurosAnualUtilizada: number;
    evolucao: MesProjecaoDto[];
}
