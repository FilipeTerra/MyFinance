namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>
    /// Pagamento extra pontual, abatido do saldo devedor no mês indicado —
    /// o caso típico é o FGTS, liberado para amortizar a cada dois anos.
    /// </summary>
    public record AmortizacaoExtraAvulsaDto
    {
        public int Mes { get; init; }
        public decimal Valor { get; init; }
    }
}
