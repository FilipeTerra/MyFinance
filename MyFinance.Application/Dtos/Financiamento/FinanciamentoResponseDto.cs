using MyFinance.Domain.Enums;

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

        /// <summary>Qual dos dois sistemas resulta em menos juros pagos.</summary>
        public SistemaAmortizacao SistemaMaisBarato { get; set; }

        /// <summary>Quanto a mais o sistema mais caro custa em juros totais, em R$.</summary>
        public decimal DiferencaTotalJuros { get; set; }

        /// <summary>Decomposição do imóvel entre entrada e valor financiado.</summary>
        public ComposicaoFinanciamentoDto Composicao { get; set; } = new();
    }
}
