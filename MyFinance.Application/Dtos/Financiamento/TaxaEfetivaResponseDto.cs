namespace MyFinance.Application.Dtos.Financiamento
{
    public class TaxaEfetivaResponseDto
    {
        public decimal TaxaNominalAnualPercentual { get; set; }
        public int CapitalizacoesPorAno { get; set; }
        public decimal TaxaEfetivaAnualPercentual { get; set; }
    }
}
