using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    /// <summary>
    /// Simula se uma meta financeira existente será atingida com um investimento.
    /// Quando <see cref="AporteMensal"/> é informado, verifica se ele é suficiente;
    /// quando omitido, calcula o aporte mensal necessário para atingir a meta.
    /// </summary>
    public record SimularMetaRequestDto
    {
        public decimal? AporteMensal { get; init; }
        public FonteTaxaJuros FonteTaxaJuros { get; init; }
        public decimal? TaxaJurosAnualPercentual { get; init; }
        public decimal? PercentualCdi { get; init; }
        public TipoAtivoCalculadora TipoAtivo { get; init; }
    }
}
