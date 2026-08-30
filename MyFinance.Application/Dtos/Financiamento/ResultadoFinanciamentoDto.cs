using System.Collections.Generic;

namespace MyFinance.Application.Dtos.Financiamento
{
    public class ResultadoFinanciamentoDto
    {
        public decimal PrimeiraParcela { get; set; }
        public decimal UltimaParcela { get; set; }
        public decimal TotalPago { get; set; }
        public decimal TotalJuros { get; set; }
        public decimal CustoEfetivoTotalPercentual { get; set; }
        public List<ParcelaFinanciamentoDto> Parcelas { get; set; } = new();
    }
}
