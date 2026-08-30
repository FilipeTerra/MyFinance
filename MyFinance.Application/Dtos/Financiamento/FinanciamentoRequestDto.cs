namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>Parâmetros de entrada para simular um financiamento pelos sistemas Price e SAC.</summary>
    public record FinanciamentoRequestDto
    {
        public decimal ValorFinanciado { get; init; }
        public decimal TaxaJurosMensalPercentual { get; init; }
        public int NumParcelas { get; init; }
    }
}
