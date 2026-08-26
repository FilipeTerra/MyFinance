using System.Text.Json.Serialization;

namespace MyFinance.Infrastructure.Integrations.Brapi.Contracts
{
    /// <summary>
    /// Módulo "financialData" do brapi. Margens e taxas de crescimento vêm como
    /// fração (0.2781 = 27,81%); valores monetários vêm em reais absolutos.
    /// </summary>
    internal sealed class BrapiFinancialData
    {
        [JsonPropertyName("totalDebt")]
        public decimal? TotalDebt { get; set; }

        [JsonPropertyName("freeCashflow")]
        public decimal? FreeCashflow { get; set; }

        [JsonPropertyName("ebitdaMargins")]
        public decimal? EbitdaMargins { get; set; }

        [JsonPropertyName("revenueGrowth")]
        public decimal? RevenueGrowth { get; set; }

        [JsonPropertyName("returnOnEquity")]
        public decimal? ReturnOnEquity { get; set; }

        [JsonPropertyName("profitMargins")]
        public decimal? ProfitMargins { get; set; }
    }
}
