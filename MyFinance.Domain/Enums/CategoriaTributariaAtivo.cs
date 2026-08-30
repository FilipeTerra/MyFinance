namespace MyFinance.Domain.Enums
{
    /// <summary>
    /// Regime de tributação aplicado ao resultado da projeção, conforme o
    /// <see cref="TipoAtivoCalculadora"/> escolhido.
    /// </summary>
    public enum CategoriaTributariaAtivo
    {
        /// <summary>CDB, RDB, Tesouro Direto — tabela regressiva de IR + IOF.</summary>
        RendaFixaTributavel = 1,

        /// <summary>LCI, LCA — isento de IR e IOF.</summary>
        RendaFixaIsenta = 2,

        /// <summary>Ganho de capital em ações: 15%, isento se o valor da venda for menor que R$ 20.000.</summary>
        GanhoCapitalAcao = 3,

        /// <summary>Ganho de capital em FII: 20%, sem isenção por valor de venda.</summary>
        GanhoCapitalFii = 4,

        /// <summary>Ganho de capital em criptomoedas: 15%, isento se o valor da venda for menor que R$ 35.000.</summary>
        GanhoCapitalCripto = 5,

        /// <summary>Fundo de ações: 15% de IR no resgate, sem isenção por valor de venda.</summary>
        GanhoCapitalFundoAcoes = 6,

        /// <summary>Fundo de renda fixa/multimercado longo prazo — come-cotas semestral a 15%, complemento no resgate.</summary>
        FundoComeCotasLongoPrazo = 7,

        /// <summary>Fundo de renda fixa curto prazo — come-cotas semestral a 20%, complemento no resgate.</summary>
        FundoComeCotasCurtoPrazo = 8,

        /// <summary>Previdência PGBL, regime regressivo definitivo — IR sobre o valor total resgatado.</summary>
        PrevidenciaPgbl = 9,

        /// <summary>Previdência VGBL, regime regressivo definitivo — IR apenas sobre o rendimento.</summary>
        PrevidenciaVgbl = 10
    }
}
