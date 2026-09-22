namespace MyFinance.Domain.Enums
{
    /// <summary>Sistema de amortização de um financiamento.</summary>
    public enum SistemaAmortizacao
    {
        /// <summary>Tabela Price: parcela contratual fixa, amortização crescente.</summary>
        Price = 1,

        /// <summary>SAC: amortização constante, parcela contratual decrescente.</summary>
        Sac = 2
    }
}
