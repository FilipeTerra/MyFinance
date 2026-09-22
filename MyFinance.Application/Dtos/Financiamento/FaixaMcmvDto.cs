namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>Condições da faixa do MCMV resolvida pela renda informada — devolvida para transparência.</summary>
    public class FaixaMcmvDto
    {
        /// <summary>"Faixa1" a "Faixa4" — nome do enum, não um texto pronto para exibir.</summary>
        public string Faixa { get; set; } = string.Empty;

        public decimal RendaMaxima { get; set; }
        public decimal TaxaAnualMinimaPercentual { get; set; }
        public decimal TaxaAnualMaximaPercentual { get; set; }
        public decimal TetoImovelReferencia { get; set; }
        public decimal EntradaMinimaPercentual { get; set; }
        public decimal? SubsidioMaximoPercentualImovel { get; set; }
        public decimal? SubsidioMaximoValor { get; set; }
    }
}
