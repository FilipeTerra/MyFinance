namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>
    /// Resultado da simulação do mesmo empréstimo nos dois sistemas de
    /// amortização (Price e SAC), mais a comparação entre eles.
    /// </summary>
    public class FinanciamentoResponseDto
    {
        public ResultadoFinanciamentoDto Price { get; set; } = new();
        public ResultadoFinanciamentoDto Sac { get; set; } = new();

        /// <summary>"Price" ou "SAC" — qual dos dois sistemas resulta em menos juros pagos.</summary>
        public string SistemaMaisBarato { get; set; } = string.Empty;

        /// <summary>Quanto a mais o sistema mais caro custa em juros totais, em R$.</summary>
        public decimal DiferencaTotalJuros { get; set; }
    }
}
