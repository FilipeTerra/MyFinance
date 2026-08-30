namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Uma linha do cronograma de amortização de um financiamento: quanto da
    /// parcela do mês é juros, quanto é abatimento do principal (amortização) e
    /// qual o saldo devedor que resta depois dela. Formato comum ao Sistema
    /// Price e ao SAC, usado por <see cref="FinanciamentoPriceCalculator"/> e
    /// <see cref="FinanciamentoSacCalculator"/>.
    /// </summary>
    public record ParcelaFinanciamento(int Numero, decimal ValorParcela, decimal Juros, decimal Amortizacao, decimal SaldoDevedor);
}
