using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    /// <summary>
    /// Fase de retirada: dado um saldo e um saque mensal (bruto) fixo, calcula
    /// quantos meses o saldo dura.
    /// </summary>
    public record CalcularDuracaoRetiradaRequestDto
    {
        public decimal SaldoInicial { get; init; }

        /// <summary>Parte do saldo inicial que corresponde ao que foi efetivamente aportado (base de custo, não ganho).</summary>
        public decimal BaseCustoInicial { get; init; }

        public decimal SaqueMensal { get; init; }

        public FonteTaxaJuros FonteTaxaJuros { get; init; }
        public decimal? TaxaJurosAnualPercentual { get; init; }
        public decimal? PercentualCdi { get; init; }
        public TipoAtivoCalculadora TipoAtivo { get; init; }
    }
}
