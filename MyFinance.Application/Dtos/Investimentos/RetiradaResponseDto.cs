using System.Collections.Generic;

namespace MyFinance.Application.Dtos.Investimentos
{
    public class RetiradaResponseDto
    {
        /// <summary>Saque mensal bruto — informado (modo duração) ou calculado (modo saque sustentável).</summary>
        public decimal SaqueMensal { get; set; }

        /// <summary>Verdadeiro quando o saque é sustentável indefinidamente (nunca esgota o saldo).</summary>
        public bool DuraParaSempre { get; set; }

        /// <summary>Mês em que o saldo se esgota. Nulo quando dura para sempre ou quando ainda não esgotou dentro do prazo simulado.</summary>
        public int? MesEsgotamento { get; set; }

        public decimal TaxaJurosAnualUtilizada { get; set; }
        public decimal? PercentualCdiUtilizado { get; set; }
        public decimal? CdiAnualUtilizado { get; set; }

        public List<MesRetiradaDto> Evolucao { get; set; } = new();
    }
}
