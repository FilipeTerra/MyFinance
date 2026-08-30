namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>Parâmetros de entrada para converter uma taxa nominal anual (APR) em taxa efetiva anual (EAR).</summary>
    public record TaxaEfetivaRequestDto
    {
        public decimal TaxaNominalAnualPercentual { get; init; }
        public int CapitalizacoesPorAno { get; init; }
    }
}
