namespace MyFinance.Domain.Enums
{
    /// <summary>
    /// Subtipos de ativo simuláveis na calculadora de projeção de investimentos.
    /// Cada subtipo é resolvido para uma <see cref="CategoriaTributariaAtivo"/> por
    /// <see cref="Services.TipoAtivoCalculadoraClassificador"/>, que determina a
    /// regra de tributação aplicada ao resultado da projeção.
    /// </summary>
    public enum TipoAtivoCalculadora
    {
        Cdb = 1,
        Rdb = 2,
        TesouroSelic = 3,
        TesouroIpca = 4,
        TesouroPrefixado = 5,
        Lci = 6,
        Lca = 7,
        Acao = 8,
        Fii = 9,
        Cripto = 10,

        /// <summary>Fundo de renda fixa/multimercado classificado como longo prazo — come-cotas a 15%.</summary>
        FundoRendaFixaLongoPrazo = 11,

        /// <summary>Fundo de renda fixa classificado como curto prazo — come-cotas a 20%.</summary>
        FundoRendaFixaCurtoPrazo = 12,

        /// <summary>Fundo multimercado — tratado como longo prazo (come-cotas a 15%), simplificação usual de mercado.</summary>
        FundoMultimercado = 13,

        /// <summary>Fundo de ações — sem come-cotas, 15% de IR no resgate, sem isenção por valor de venda.</summary>
        FundoAcoes = 14,

        /// <summary>Previdência PGBL, regime regressivo definitivo — IR sobre o valor total resgatado.</summary>
        Pgbl = 15,

        /// <summary>Previdência VGBL, regime regressivo definitivo — IR apenas sobre o rendimento.</summary>
        Vgbl = 16
    }
}
