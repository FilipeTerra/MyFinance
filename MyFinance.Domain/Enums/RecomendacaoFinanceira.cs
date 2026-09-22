namespace MyFinance.Domain.Enums
{
    /// <summary>O que compensa mais financeiramente entre pagar uma dívida a mais ou investir o mesmo dinheiro.</summary>
    public enum RecomendacaoFinanceira
    {
        /// <summary>Amortizar economiza mais em juros do que o investimento renderia líquido no mesmo prazo.</summary>
        Amortizar = 1,

        /// <summary>O investimento rende líquido mais do que os juros que a amortização economizaria.</summary>
        Investir = 2
    }
}
