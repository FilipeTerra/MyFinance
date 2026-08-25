namespace MyFinance.Application.Dtos.Investimentos
{
    public class MesProjecaoDto
    {
        public int Mes { get; set; }
        public decimal ValorAcumulado { get; set; }
        public decimal TotalAportadoAcumulado { get; set; }
        public decimal JurosAcumulado { get; set; }
    }
}
