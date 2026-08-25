using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Infrastructure.Services
{
    public class StockMarketIntegrationService : IStockMarketIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StockMarketIntegrationService> _logger;

        public StockMarketIntegrationService(HttpClient httpClient, ILogger<StockMarketIntegrationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<decimal?> GetQuoteAsync(string ticker)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/market/quote/{ticker}");
                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<QuotePythonResponse>();
                return result?.Success == true ? result.PrecoAtualBrl : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar cotação atual do ticker {Ticker}.", ticker);
                return null;
            }
        }

        public async Task<IEnumerable<CotacaoPontoDto>> GetHistoryAsync(string ticker, int meses)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/market/history/{ticker}?meses={meses}");
                if (!response.IsSuccessStatusCode)
                    return Array.Empty<CotacaoPontoDto>();

                var result = await response.Content.ReadFromJsonAsync<HistoryPythonResponse>();
                if (result?.Success != true || result.Historico == null)
                    return Array.Empty<CotacaoPontoDto>();

                return result.Historico
                    .Select(p => new CotacaoPontoDto { Data = p.Data, Valor = p.Valor })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar histórico de cotações do ticker {Ticker}.", ticker);
                return Array.Empty<CotacaoPontoDto>();
            }
        }

        public async Task<decimal?> GetTaxaSelicAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/market/selic");
                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<SelicPythonResponse>();
                return result?.Success == true ? result.SelicAnualPct : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar a taxa Selic.");
                return null;
            }
        }

        private class QuotePythonResponse
        {
            public bool Success { get; set; }

            [JsonPropertyName("preco_atual_brl")]
            public decimal PrecoAtualBrl { get; set; }
        }

        private class HistoryPythonResponse
        {
            public bool Success { get; set; }
            public List<HistoricoPontoPythonResponse>? Historico { get; set; }
        }

        private class HistoricoPontoPythonResponse
        {
            public DateTime Data { get; set; }
            public decimal Valor { get; set; }
        }

        private class SelicPythonResponse
        {
            public bool Success { get; set; }

            [JsonPropertyName("selic_anual_pct")]
            public decimal SelicAnualPct { get; set; }
        }
    }
}
