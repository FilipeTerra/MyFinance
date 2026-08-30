namespace MyFinance.Application.Dtos.Investimentos
{
    public class MesRetiradaDto
    {
        public int Mes { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal SaqueBruto { get; set; }
        public decimal AliquotaImpostoPercentual { get; set; }
        public decimal ValorImposto { get; set; }
        public decimal SaqueLiquido { get; set; }
        public decimal SaldoFinal { get; set; }
    }
}
