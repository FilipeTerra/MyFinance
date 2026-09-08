using System.Text.Json.Serialization;

namespace MyFinance.Infrastructure.Integrations.AiAgent.Contracts
{
    /// <summary>
    /// Resposta de POST /api/ai/extract-statement: extração crua do documento,
    /// sem resolução de categoria — quem categoriza é a API.
    /// </summary>
    internal sealed class ExtractStatementResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<ExtractedTransactionContract>? Transactions { get; set; }
    }

    internal sealed class ExtractedTransactionContract
    {
        /// <summary>Data no formato YYYY-MM-DD.</summary>
        public string? Date { get; set; }

        public string? Description { get; set; }

        /// <summary>Valor absoluto; o sinal vem de <see cref="Tipo"/>.</summary>
        public decimal Valor { get; set; }

        /// <summary>"receita" ou "despesa".</summary>
        public string? Tipo { get; set; }

        /// <summary>Categoria informada pelo próprio documento, quando houver.</summary>
        public string? Categoria { get; set; }
    }

    /// <summary>Resposta de POST /api/ai/suggest-categories.</summary>
    internal sealed class SuggestCategoriesResponse
    {
        public bool Success { get; set; }

        [JsonPropertyName("suggestions")]
        public Dictionary<string, string>? Suggestions { get; set; }
    }
}
