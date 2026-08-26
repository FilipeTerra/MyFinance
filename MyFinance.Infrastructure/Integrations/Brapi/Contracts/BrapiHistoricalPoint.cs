using System.Text.Json.Serialization;

namespace MyFinance.Infrastructure.Integrations.Brapi.Contracts
{
    internal sealed class BrapiHistoricalPoint
    {
        /// <summary>Timestamp UNIX em SEGUNDOS (não milissegundos).</summary>
        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("close")]
        public decimal? Close { get; set; }

        /// <summary>
        /// Fechamento ajustado por splits e proventos. É este o campo usado na série
        /// histórica: mantém continuidade com o histórico gravado anteriormente pelo
        /// yfinance (que também é ajustado) e evita variação espúria em data de split.
        /// </summary>
        [JsonPropertyName("adjustedClose")]
        public decimal? AdjustedClose { get; set; }
    }
}
