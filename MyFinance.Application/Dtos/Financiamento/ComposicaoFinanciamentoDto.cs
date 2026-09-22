namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>
    /// Como o valor do imóvel se decompõe entre o que o comprador paga à vista
    /// (entrada) e o que o banco financia.
    /// </summary>
    public class ComposicaoFinanciamentoDto
    {
        public decimal ValorImovel { get; set; }
        public decimal Entrada { get; set; }
        public decimal EntradaPercentual { get; set; }
        public decimal ValorFinanciado { get; set; }

        public decimal Itbi { get; set; }
        public decimal CustosCartorio { get; set; }

        /// <summary>Tudo que precisa estar em caixa além do financiamento: entrada + ITBI + cartório.</summary>
        public decimal DesembolsoInicial { get; set; }

        /// <summary>Subsídio do MCMV, em R$. Abate o financiado, mas não é pago pelo comprador.</summary>
        public decimal Subsidio { get; set; }
    }
}
