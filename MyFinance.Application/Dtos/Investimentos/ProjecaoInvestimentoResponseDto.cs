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

        public List<MesProjecaoDto> Evolucao { get; set; } = new();
    }
}
