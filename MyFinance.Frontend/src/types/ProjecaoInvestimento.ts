export interface CalcularProjecaoRequestDto {
    aporteInicial: number;
    aporteMensal: number;
    prazoMeses: number;
    /** Obrigatório quando usarTaxaSelic é falso. */
    taxaJurosAnualPercentual?: number;
    /** Quando verdadeiro, ignora taxaJurosAnualPercentual e usa a Selic real vigente. */
    usarTaxaSelic: boolean;
    /** Quando verdadeiro, aplica a tabela regressiva de IR sobre o rendimento (CDB, Tesouro Direto, fundos DI/RF). */
    aplicarImpostoRenda: boolean;
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
    /** Alíquota de IOF (%) sobre o rendimento, quando o resgate simulado ocorre em menos de 30 dias. */
    aliquotaIofPercentual: number;
    /** Valor de IOF retido sobre o rendimento, em reais. */
    valorIof: number;
    /** Alíquota de IR (%) aplicada sobre o rendimento (já líquido de IOF). Zero quando isento ou não aplicável. */
    aliquotaImpostoRendaPercentual: number;
    /** Valor de IR retido sobre o rendimento, em reais. */
    valorImpostoRenda: number;
    /** Valor final projetado já descontado o IOF e o IR. */
    valorFinalLiquido: number;
    evolucao: MesProjecaoDto[];
}
