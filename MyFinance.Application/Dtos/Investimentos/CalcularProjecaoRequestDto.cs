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

        /// <summary>
        /// Quando verdadeiro, aplica a tabela regressiva de Imposto de Renda sobre o
        /// rendimento (CDB, Tesouro Direto, fundos DI/RF). Deixe falso para simular
        /// um ativo isento de IR (LCI, LCA, poupança, debêntures incentivadas).
        /// </summary>
        public bool AplicarImpostoRenda { get; init; }
    }
}
