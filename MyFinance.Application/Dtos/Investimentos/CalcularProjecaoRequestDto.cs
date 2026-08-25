namespace MyFinance.Application.Dtos.Investimentos
{
    public record CalcularProjecaoRequestDto
    {
        public decimal AporteInicial { get; init; }
        public decimal AporteMensal { get; init; }
        public int PrazoMeses { get; init; }

        /// <summary>
        /// Taxa de juros anual (%) a simular. Obrigatória quando <see cref="UsarTaxaSelic"/> é falso.
        /// </summary>
        public decimal? TaxaJurosAnualPercentual { get; init; }

        /// <summary>
        /// Quando verdadeiro, ignora <see cref="TaxaJurosAnualPercentual"/> e usa a taxa Selic
        /// real vigente (buscada via Banco Central) como taxa de referência do Tesouro Direto.
        /// </summary>
        public bool UsarTaxaSelic { get; init; }
    }
}
