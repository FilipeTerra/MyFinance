namespace MyFinance.Domain.Enums
{
    /// <summary>O que um pagamento extra abate no contrato de financiamento.</summary>
    public enum ModoAmortizacaoExtra
    {
        /// <summary>
        /// Mantém a parcela e antecipa a quitação, abatendo as últimas parcelas.
        /// É o que mais economiza juros.
        /// </summary>
        ReduzirPrazo = 1,

        /// <summary>
        /// Mantém o prazo original e recalcula a parcela sobre o saldo restante.
        /// Economiza menos juros, mas alivia o desembolso mensal.
        /// </summary>
        ReduzirParcela = 2
    }
}
