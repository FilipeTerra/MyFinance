import type { TipoAtivoCalculadora } from './TipoAtivoCalculadora';
import type { CategoriaTributariaAtivo } from './CategoriaTributariaAtivo';
import type { FonteTaxaJuros } from './FonteTaxaJuros';
import type { ReajusteAporteModo } from './ReajusteAporteModo';

export interface AporteExtraDto {
    mes: number;
    valor: number;
}

export interface CalcularProjecaoRequestDto {
    aporteInicial: number;
    aporteMensal: number;
    prazoMeses: number;
    /** De onde vem a taxa de juros anual simulada. */
    fonteTaxaJuros: FonteTaxaJuros;
    /** Obrigatório quando fonteTaxaJuros é Manual. */
    taxaJurosAnualPercentual?: number;
    /** Percentual do CDI (ex.: 110). Obrigatório quando fonteTaxaJuros é PercentualCdi. */
    percentualCdi?: number;
    /** Subtipo de ativo simulado — determina o regime de tributação aplicado ao resultado. */
    tipoAtivo: TipoAtivoCalculadora;
    /** Aportes avulsos (13º salário, bônus) somados ao aporte mensal no mês indicado. */
    aportesExtras?: AporteExtraDto[];
    /** Como o aporte mensal recorrente é reajustado a cada 12 meses. */
    reajusteAporteModo?: ReajusteAporteModo;
    /** Obrigatório quando reajusteAporteModo é PercentualFixo. */
    reajusteAporteAnualPercentual?: number;
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
    /** Taxa de juros anual (%) efetivamente usada — manual, Selic real ou % do CDI. */
    taxaJurosAnualUtilizada: number;
    /** Alíquota de IOF (%) sobre o rendimento, quando o resgate simulado ocorre em menos de 30 dias. */
    aliquotaIofPercentual: number;
    /** Valor de IOF retido sobre o rendimento, em reais. */
    valorIof: number;
    /** Alíquota de IR (%) — tabela regressiva (renda fixa) ou alíquota fixa (ganho de capital). Zero quando isento. */
    aliquotaImpostoRendaPercentual: number;
    /** Valor de IR retido sobre o rendimento, em reais. */
    valorImpostoRenda: number;
    /** Valor final projetado já descontado o IOF e o IR. */
    valorFinalLiquido: number;
    /** Regime de tributação resolvido a partir do tipoAtivo informado. */
    categoriaTributaria: CategoriaTributariaAtivo;
    /** Verdadeiro quando o imposto de ganho de capital foi zerado pela isenção de valor de venda. */
    isentoPorFaixaDeVenda: boolean;
    /** Alíquota (%) de come-cotas antecipada semestralmente. Zero fora de fundos com come-cotas. */
    aliquotaComeCotasPercentual: number;
    /** Total já retido via come-cotas ao longo da simulação — antecipação do IR devido. */
    valorComeCotasRetido: number;
    /** Percentual do CDI simulado (ex.: 110). Ausente fora do modo "% do CDI". */
    percentualCdiUtilizado?: number;
    /** CDI anual (%) usado para derivar a taxa efetiva. Ausente fora do modo "% do CDI". */
    cdiAnualUtilizado?: number;
    /** IPCA anual (%) usado para calcular a rentabilidade real. Ausente no modo manual. */
    ipcaAnualUtilizado?: number;
    /** Rentabilidade real anual (%), líquida de inflação. Ausente no modo manual. */
    rentabilidadeRealAnualPercentual?: number;
    evolucao: MesProjecaoDto[];
}
