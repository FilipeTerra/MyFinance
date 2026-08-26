using System.Text.Json.Serialization;

namespace MyFinance.Infrastructure.Integrations.AiAgent.Contracts
{
    internal sealed class ProactiveInsightPythonResponse
    {
        public bool Success { get; set; }
        public string? Erro { get; set; }

        [JsonPropertyName("exibir_card")]
        public bool ExibirCard { get; set; }

        [JsonPropertyName("tipo_card")]
        public string? TipoCard { get; set; }

        [JsonPropertyName("curiosidade")]
        public string? Curiosidade { get; set; }

        [JsonPropertyName("informacao")]
        public string? Informacao { get; set; }

        [JsonPropertyName("sugestao")]
        public string? Sugestao { get; set; }

        [JsonPropertyName("valor_ideal")]
        public decimal ValorIdeal { get; set; }

        [JsonPropertyName("valor_atual")]
        public decimal ValorAtual { get; set; }

        [JsonPropertyName("valor_faltante")]
        public decimal ValorFaltante { get; set; }

        [JsonPropertyName("percentual_atingido")]
        public decimal PercentualAtingido { get; set; }
    }

    internal sealed class LifestyleInsightPythonResponse
    {
        public bool Success { get; set; }
        public string? Erro { get; set; }
        public bool Alerta { get; set; }

        [JsonPropertyName("curiosidade")]
        public string? Curiosidade { get; set; }

        [JsonPropertyName("informacao")]
        public string? Informacao { get; set; }

        [JsonPropertyName("sugestao")]
        public string? Sugestao { get; set; }

        [JsonPropertyName("percentual_renda_estilo_vida")]
        public decimal? PercentualRendaEstiloVida { get; set; }

        [JsonPropertyName("variacao_estilo_vida_pct")]
        public decimal? VariacaoEstiloVidaPct { get; set; }

        [JsonPropertyName("variacao_aportes_pct")]
        public decimal? VariacaoAportesPct { get; set; }
    }
}
