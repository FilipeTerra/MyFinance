using System.Text.Json.Serialization;

namespace MyFinance.Infrastructure.Integrations.Brapi.Contracts
{
    /// <summary>
    /// Módulo "defaultKeyStatistics" do brapi. Todos os campos são nullable:
    /// o provedor devolve null para indicadores que não se aplicam ao ativo
    /// (ex: EV/EBITDA de banco), e null deve trafegar como null — nunca 0.0.
    /// </summary>
    internal sealed class BrapiDefaultKeyStatistics
    {
        [JsonPropertyName("trailingPE")]
        public decimal? TrailingPE { get; set; }

        [JsonPropertyName("enterpriseToEbitda")]
        public decimal? EnterpriseToEbitda { get; set; }

        /// <summary>Fração (0.09 = 9%) — exige ×100 para virar percentual.</summary>
        [JsonPropertyName("dividendYield")]
        public decimal? DividendYield { get; set; }

        [JsonPropertyName("profitMargins")]
        public decimal? ProfitMargins { get; set; }
    }
}
