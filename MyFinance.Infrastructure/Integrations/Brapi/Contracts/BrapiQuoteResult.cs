using System.Text.Json.Serialization;

namespace MyFinance.Infrastructure.Integrations.Brapi.Contracts
{
    internal sealed class BrapiQuoteResult
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("regularMarketPrice")]
        public decimal? RegularMarketPrice { get; set; }

        [JsonPropertyName("fiftyTwoWeekLow")]
        public decimal? FiftyTwoWeekLow { get; set; }

        [JsonPropertyName("fiftyTwoWeekHigh")]
        public decimal? FiftyTwoWeekHigh { get; set; }

        [JsonPropertyName("historicalDataPrice")]
        public List<BrapiHistoricalPoint>? HistoricalDataPrice { get; set; }

        [JsonPropertyName("defaultKeyStatistics")]
        public BrapiDefaultKeyStatistics? DefaultKeyStatistics { get; set; }

        [JsonPropertyName("financialData")]
        public BrapiFinancialData? FinancialData { get; set; }
    }
}
