using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    /// <summary>
    /// "Meta reversa": dado um valor-alvo e um aporte mensal fixo, pede-se o
    /// prazo (em meses) necessário para atingi-lo.
    /// </summary>
    public record CalcularPrazoNecessarioRequestDto
    {
        public decimal AporteInicial { get; init; }
        public decimal AporteMensal { get; init; }

        /// <summary>Valor líquido (já descontados os tributos) que se deseja atingir.</summary>
        public decimal ValorAlvo { get; init; }

        public FonteTaxaJuros FonteTaxaJuros { get; init; }
        public decimal? TaxaJurosAnualPercentual { get; init; }
        public decimal? PercentualCdi { get; init; }
        public TipoAtivoCalculadora TipoAtivo { get; init; }
    }
}
