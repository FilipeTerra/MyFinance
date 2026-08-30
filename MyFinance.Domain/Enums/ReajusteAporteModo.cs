namespace MyFinance.Domain.Enums
{
    /// <summary>Como o aporte mensal recorrente é reajustado a cada 12 meses simulados.</summary>
    public enum ReajusteAporteModo
    {
        /// <summary>Aporte mensal constante, sem reajuste.</summary>
        Nenhum = 1,

        /// <summary>Reajuste por um percentual fixo ao ano, informado pelo usuário.</summary>
        PercentualFixo = 2,

        /// <summary>Reajuste pelo IPCA anual real, buscado via Banco Central.</summary>
        Ipca = 3
    }
}
