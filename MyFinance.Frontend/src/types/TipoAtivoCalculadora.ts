/**
 * Subtipos de ativo simuláveis na calculadora de projeção.
 * Corresponde ao enum TipoAtivoCalculadora.cs no backend (serializado como número).
 */
export const TipoAtivoCalculadora = {
    Cdb: 1,
    Rdb: 2,
    TesouroSelic: 3,
    TesouroIpca: 4,
    TesouroPrefixado: 5,
    Lci: 6,
    Lca: 7,
    Acao: 8,
    Fii: 9,
    Cripto: 10,
    FundoRendaFixaLongoPrazo: 11,
    FundoRendaFixaCurtoPrazo: 12,
    FundoMultimercado: 13,
    FundoAcoes: 14,
    Pgbl: 15,
    Vgbl: 16,
} as const;

export type TipoAtivoCalculadora = (typeof TipoAtivoCalculadora)[keyof typeof TipoAtivoCalculadora];
