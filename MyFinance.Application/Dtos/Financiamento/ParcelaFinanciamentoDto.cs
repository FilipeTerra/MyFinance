namespace MyFinance.Application.Dtos.Financiamento
{
    public class ParcelaFinanciamentoDto
    {
        public int Numero { get; set; }
        public decimal ValorParcela { get; set; }
        public decimal Juros { get; set; }
        public decimal Amortizacao { get; set; }
        public decimal SaldoDevedor { get; set; }
    }
}
