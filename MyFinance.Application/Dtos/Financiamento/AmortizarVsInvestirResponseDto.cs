using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Financiamento
{
    public class AmortizarVsInvestirResponseDto
    {
        public RecomendacaoFinanceira Recomendacao { get; set; }

        /// <summary>Juros que deixariam de ser pagos se o valor disponível virasse amortização extra (prazo reduzido).</summary>
        public decimal EconomiaJurosAmortizando { get; set; }

        /// <summary>Valor final líquido (já com IR/IOF) se o mesmo valor fosse investido pelo mesmo prazo.</summary>
        public decimal ValorFinalLiquidoInvestindo { get; set; }

        /// <summary>Investir menos amortizar. Positivo favorece investir; negativo favorece amortizar.</summary>
        public decimal Diferenca { get; set; }

        /// <summary>Projeção completa do cenário de investimento, para quem quiser o detalhamento de impostos.</summary>
        public ProjecaoInvestimentoResponseDto ProjecaoInvestindo { get; set; } = new();
    }
}
