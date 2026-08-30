using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    /// <summary>
    /// Fase de retirada: dado um saldo e um prazo de retirada desejado, calcula
    /// o maior saque mensal (bruto) que mantém o saldo positivo até o fim do prazo.
    /// </summary>
    public record CalcularSaqueSustentavelRequestDto
    {
        public decimal SaldoInicial { get; init; }

        /// <summary>Parte do saldo inicial que corresponde ao que foi efetivamente aportado (base de custo, não ganho).</summary>
        public decimal BaseCustoInicial { get; init; }

        public int PrazoMeses { get; init; }

        public FonteTaxaJuros FonteTaxaJuros { get; init; }
        public decimal? TaxaJurosAnualPercentual { get; init; }
        public decimal? PercentualCdi { get; init; }
        public TipoAtivoCalculadora TipoAtivo { get; init; }
    }
}
