namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Uma linha do cronograma de amortização de um financiamento: quanto da
    /// parcela do mês é juros, quanto é abatimento do principal (amortização) e
    /// qual o saldo devedor que resta depois dela. Formato comum ao Sistema
    /// Price e ao SAC, usado por <see cref="FinanciamentoPriceCalculator"/> e
    /// <see cref="FinanciamentoSacCalculator"/>.
    /// </summary>
    /// <param name="ValorParcela">Parcela contratual do mês (juros + amortização), sem extras nem encargos.</param>
    /// <param name="AmortizacaoExtra">Pagamento extra do mês, abatido direto do principal.</param>
    /// <param name="SeguroMip">Seguro de morte e invalidez do mês, calculado sobre o saldo devedor.</param>
    /// <param name="SeguroDfi">Seguro de danos físicos ao imóvel do mês.</param>
    /// <param name="TaxaAdministracao">Tarifa mensal de administração do contrato.</param>
    public record ParcelaFinanciamento(
        int Numero,
        decimal ValorParcela,
        decimal Juros,
        decimal Amortizacao,
        decimal SaldoDevedor,
        decimal AmortizacaoExtra = 0m,
        decimal SeguroMip = 0m,
        decimal SeguroDfi = 0m,
        decimal TaxaAdministracao = 0m)
    {
        /// <summary>Soma dos seguros obrigatórios do mês (MIP + DFI).</summary>
        public decimal Seguros => SeguroMip + SeguroDfi;

        /// <summary>
        /// Desembolso real do mês: a parcela contratual mais o pagamento extra,
        /// os seguros e a taxa de administração. É este o valor que aparece no
        /// boleto — e o que deve ser comparado com a renda.
        /// </summary>
        public decimal ParcelaTotal => ValorParcela + AmortizacaoExtra + Seguros + TaxaAdministracao;
    }
}
