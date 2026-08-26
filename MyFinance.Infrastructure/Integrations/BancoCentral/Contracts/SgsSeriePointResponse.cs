using System.Text.Json.Serialization;

namespace MyFinance.Infrastructure.Integrations.BancoCentral.Contracts
{
    /// <summary>
    /// Ponto de uma série temporal do SGS/BCB. A API devolve um array destes,
    /// com ambos os campos como string: data em "dd/MM/yyyy" e valor com ponto
    /// decimal (ex: "14.25") — exige InvariantCulture no parse.
    /// </summary>
    internal sealed class SgsSeriePointResponse
    {
        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("valor")]
        public string? Valor { get; set; }
    }
}
