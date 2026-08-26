using System.Text.Json.Serialization;

namespace MyFinance.Infrastructure.Integrations.Brapi.Contracts
{
    internal sealed class BrapiQuoteResponse
    {
        [JsonPropertyName("results")]
        public List<BrapiQuoteResult>? Results { get; set; }

        /// <summary>Preenchido apenas em resposta de erro (ex: MISSING_TOKEN).</summary>
        [JsonPropertyName("error")]
        public bool? Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}
