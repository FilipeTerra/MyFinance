using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    /// <summary>
    /// "Meta reversa": dado um valor-alvo e um prazo fixo, pede-se o aporte
    /// mensal necessário (com o aporte inicial já informado) para atingi-lo.
    /// </summary>
    public record CalcularAporteNecessarioRequestDto
    {
        public decimal AporteInicial { get; init; }
        public int PrazoMeses { get; init; }

        /// <summary>Valor líquido (já descontados os tributos) que se deseja atingir ao final do prazo.</summary>
        public decimal ValorAlvo { get; init; }

        public FonteTaxaJuros FonteTaxaJuros { get; init; }
        public decimal? TaxaJurosAnualPercentual { get; init; }
        public decimal? PercentualCdi { get; init; }
        public TipoAtivoCalculadora TipoAtivo { get; init; }
    }
}
