namespace MyFinance.Domain.Enums
{
    /// <summary>
    /// De onde vem a taxa de juros anual usada na projeção.
    /// </summary>
    public enum FonteTaxaJuros
    {
        /// <summary>Taxa informada manualmente pelo usuário.</summary>
        Manual = 1,

        /// <summary>Taxa Selic real vigente, buscada via Banco Central.</summary>
        Selic = 2,

        /// <summary>Percentual do CDI vigente (ex.: 110% do CDI), buscado via Banco Central.</summary>
        PercentualCdi = 3
    }
}
