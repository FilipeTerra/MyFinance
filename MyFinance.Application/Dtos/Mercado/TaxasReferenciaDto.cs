namespace MyFinance.Application.Dtos.Mercado
{
    /// <summary>
    /// Taxas de referência da economia brasileira, derivadas das séries do
    /// Banco Central (Selic meta e IPCA 12 meses).
    /// </summary>
    public class TaxasReferenciaDto
    {
        public decimal SelicAnualPct { get; set; }
        public decimal SelicMensalPct { get; set; }
        public decimal IpcaAnualPct { get; set; }
        public decimal IpcaMensalPct { get; set; }

        /// <summary>Juros reais pela equação de Fisher: (1+selic)/(1+ipca) - 1.</summary>
        public decimal JurosRealAnualPct { get; set; }

        /// <summary>CDI estimado — convencionalmente a Selic meta menos um pequeno spread.</summary>
        public decimal CdiAnualPct { get; set; }

        public string DataReferenciaSelic { get; set; } = string.Empty;
        public string DataReferenciaIpca { get; set; } = string.Empty;

        /// <summary>
        /// Origem do dado — distingue consulta em tempo real de valor de fallback.
        /// É exposto ao agente de IA como sinalização honesta de qualidade do dado.
        /// </summary>
        public string Fonte { get; set; } = string.Empty;
    }
}
