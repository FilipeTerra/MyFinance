namespace MyFinance.Infrastructure.Integrations.BancoCentral
{
    /// <summary>
    /// Configuração da API de séries temporais do Banco Central (SGS).
    /// </summary>
    public class BancoCentralOptions
    {
        public const string SectionName = "ExternalServices:BancoCentral";

        public string BaseUrl { get; set; } = "https://api.bcb.gov.br/dados/serie/";

        /// <summary>Série 432 — Selic meta definida pelo Copom.</summary>
        public int SerieSelicMeta { get; set; } = 432;

        /// <summary>Série 13522 — IPCA acumulado em 12 meses.</summary>
        public int SerieIpca12Meses { get; set; } = 13522;

        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>Valor usado quando a consulta da Selic falha — o serviço nunca retorna nulo.</summary>
        public decimal FallbackSelicAnualPct { get; set; } = 14.25m;

        /// <summary>Valor usado quando a consulta do IPCA falha.</summary>
        public decimal FallbackIpcaAnualPct { get; set; } = 4.72m;

        /// <summary>Spread convencional do CDI em relação à Selic meta (pontos percentuais).</summary>
        public decimal SpreadCdiPp { get; set; } = 0.10m;
    }
}
