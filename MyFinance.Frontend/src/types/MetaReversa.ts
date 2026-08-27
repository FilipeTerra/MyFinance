import type { TipoAtivoCalculadora } from './TipoAtivoCalculadora';
import type { FonteTaxaJuros } from './FonteTaxaJuros';
import type { ProjecaoInvestimentoResponseDto } from './ProjecaoInvestimento';

interface TaxaConfig {
    fonteTaxaJuros: FonteTaxaJuros;
    taxaJurosAnualPercentual?: number;
    percentualCdi?: number;
    tipoAtivo: TipoAtivoCalculadora;
}

export interface CalcularAporteNecessarioRequestDto extends TaxaConfig {
    aporteInicial: number;
    prazoMeses: number;
    /** Valor líquido (já descontados os tributos) que se deseja atingir ao final do prazo. */
    valorAlvo: number;
}

export interface CalcularPrazoNecessarioRequestDto extends TaxaConfig {
    aporteInicial: number;
    aporteMensal: number;
    /** Valor líquido (já descontados os tributos) que se deseja atingir. */
    valorAlvo: number;
}

export interface AporteNecessarioResponseDto {
    aporteMensalNecessario: number;
    projecao: ProjecaoInvestimentoResponseDto;
}

export interface PrazoNecessarioResponseDto {
    atingivel: boolean;
    prazoMesesNecessario?: number;
    projecao?: ProjecaoInvestimentoResponseDto;
}

export interface SimularMetaRequestDto extends TaxaConfig {
    /** Quando informado, verifica se esse aporte mensal atinge a meta. Quando omitido, calcula o aporte necessário. */
    aporteMensal?: number;
}

export interface SimularMetaResponseDto {
    prazoMesesRestante: number;
    atinge: boolean;
    aporteMensalNecessario?: number;
    diferencaParaMeta: number;
    projecao: ProjecaoInvestimentoResponseDto;
}
