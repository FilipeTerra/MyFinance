/**
 * Regime de tributação resolvido a partir do TipoAtivoCalculadora.
 * Corresponde ao enum CategoriaTributariaAtivo.cs no backend (serializado como número).
 */
export const CategoriaTributariaAtivo = {
    RendaFixaTributavel: 1,
    RendaFixaIsenta: 2,
    GanhoCapitalAcao: 3,
    GanhoCapitalFii: 4,
    GanhoCapitalCripto: 5,
    GanhoCapitalFundoAcoes: 6,
    FundoComeCotasLongoPrazo: 7,
    FundoComeCotasCurtoPrazo: 8,
    PrevidenciaPgbl: 9,
    PrevidenciaVgbl: 10,
} as const;

export type CategoriaTributariaAtivo = (typeof CategoriaTributariaAtivo)[keyof typeof CategoriaTributariaAtivo];
