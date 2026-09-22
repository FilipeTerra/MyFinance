namespace MyFinance.Domain.Enums
{
    /// <summary>
    /// Faixa de renda do programa Minha Casa Minha Vida. Os limites de renda e as
    /// condições de cada faixa vivem em <c>MinhaCasaMinhaVidaTabela</c>, não aqui —
    /// o enum só nomeia a faixa.
    /// </summary>
    public enum FaixaMinhaCasaMinhaVida
    {
        /// <summary>Renda familiar bruta mensal até R$ 3.200 — a faixa com maior subsídio (até 95% do imóvel).</summary>
        Faixa1 = 1,

        /// <summary>Renda familiar bruta mensal até R$ 5.000 — subsídio de até R$ 55.000.</summary>
        Faixa2 = 2,

        /// <summary>Renda familiar bruta mensal até R$ 9.600 — sem subsídio, taxa ainda reduzida.</summary>
        Faixa3 = 3,

        /// <summary>Renda familiar bruta mensal até R$ 13.000 — sem subsídio, a faixa mais cara do programa.</summary>
        Faixa4 = 4
    }
}
