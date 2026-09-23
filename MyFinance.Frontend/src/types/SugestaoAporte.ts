import type { TipoAtivoCalculadora } from './TipoAtivoCalculadora';
import type { FonteTaxaJuros } from './FonteTaxaJuros';
import type { ExpenseNature } from './ExpenseNature';

/** De onde veio a renda mensal usada no diagnóstico. */
export const FonteRenda = {
    Indisponivel: 0,
    /** Salário informado pelo usuário no perfil. */
    PerfilDeclarado: 1,
    /** Média das receitas efetivamente lançadas no período. */
    TransacoesRealizadas: 2,
} as const;

export type FonteRenda = (typeof FonteRenda)[keyof typeof FonteRenda];

export interface SugestaoAporteRequestDto {
    aporteInicial: number;
    prazoMeses: number;
    valorAlvo: number;
    /** Aporte já exibido na tela; evita repetir a busca binária da meta reversa no servidor. */
    aporteMensalNecessario?: number;
    fonteTaxaJuros: FonteTaxaJuros;
    taxaJurosAnualPercentual?: number;
    percentualCdi?: number;
    tipoAtivo: TipoAtivoCalculadora;
}

export interface CorteSugeridoDto {
    categoryId: string;
    categoryName: string;
    gastoAtual: number;
    valorCorte: number;
    gastoDepoisDoCorte: number;
}

export interface CategoriaNaoClassificadaDto {
    categoryId: string;
    categoryName: string;
    gastoMensalMedio: number;
    nature: ExpenseNature;
}

export interface ReservaEmergenciaDto {
    adequada: boolean;
    valorIdeal: number;
    valorAtual: number;
    valorFaltante: number;
    percentualAtingido: number;
    mesesCobertos: number;
}

export interface CenariosAlternativosDto {
    aporteSustentavel: number;
    prazoMesesAlternativo?: number;
    alvoInatingivelNoPrazoMaximo: boolean;
    valorAlvoAlternativo?: number;
}

export interface SugestaoAporteResponseDto {
    aporteMensalNecessario: number;
    cabe: boolean;
    rendaMensal: number;
    fonteRenda: FonteRenda;
    despesaMensalMedia: number;
    aportesMensaisMedios: number;
    sobraLivre: number;
    deficit: number;
    folgaRestante: number;
    capacidadeMaxima: number;
    percentualDaRenda: number;
    cortes: CorteSugeridoDto[];
    naoClassificadas: CategoriaNaoClassificadaDto[];
    reserva?: ReservaEmergenciaDto;
    cenarios?: CenariosAlternativosDto;
    inicioAnalise: string;
    fimAnalise: string;
    mesesAnalisados: number;
    /** Menos de dois meses de dados: as médias são pouco confiáveis. */
    historicoInsuficiente: boolean;
    textoConsultivo: string;
    iaUsada: boolean;
    /** A IA não respondeu e o texto veio do template. Não é erro. */
    iaIndisponivel: boolean;
}

export interface UpdateCategoryNaturesRequestDto {
    items: { categoryId: string; nature: ExpenseNature }[];
}
