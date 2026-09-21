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
    }
}
