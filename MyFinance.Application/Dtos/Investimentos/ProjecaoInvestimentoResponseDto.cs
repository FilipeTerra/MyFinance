using System.Collections.Generic;

namespace MyFinance.Application.Dtos.Investimentos
{
    public class ProjecaoInvestimentoResponseDto
    {
        public decimal ValorFinal { get; set; }
        public decimal TotalAportado { get; set; }
        public decimal TotalJuros { get; set; }
        public decimal RentabilidadePercentual { get; set; }

        /// <summary>
        /// Taxa de juros anual (%) efetivamente usada no cálculo — a informada
        /// manualmente, ou a Selic real buscada via Banco Central.
        /// </summary>
        public decimal TaxaJurosAnualUtilizada { get; set; }

        /// <summary>
        /// Alíquota de IOF (%) aplicada sobre o rendimento quando o resgate
        /// simulado ocorre em menos de 30 dias corridos. Zero na maioria dos
        /// casos, já que o prazo mínimo simulado é de 1 mês.
        /// </summary>
        public decimal AliquotaIofPercentual { get; set; }

        /// <summary>Valor de IOF retido sobre o rendimento, em reais.</summary>
        public decimal ValorIof { get; set; }

        /// <summary>
        /// Alíquota de IR (%) aplicada sobre o rendimento (já líquido de IOF),
        /// conforme a tabela regressiva e o prazo simulado. Zero quando o
        /// investimento é isento ou <see cref="CalcularProjecaoRequestDto.AplicarImpostoRenda"/> é falso.
        /// </summary>
        public decimal AliquotaImpostoRendaPercentual { get; set; }

        /// <summary>Valor de IR retido sobre o rendimento, em reais.</summary>
        public decimal ValorImpostoRenda { get; set; }

        /// <summary>Valor final projetado já descontado o IOF e o IR.</summary>
        public decimal ValorFinalLiquido { get; set; }

        public List<MesProjecaoDto> Evolucao { get; set; } = new();
    }
}
