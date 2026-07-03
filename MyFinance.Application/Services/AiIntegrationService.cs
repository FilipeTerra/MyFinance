using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services
{
    public class AiIntegrationService : IAiIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ICategoryRepository _categoryRepository; 

        public AiIntegrationService(HttpClient httpClient, ICategoryRepository categoryRepository)
        {
            _httpClient = httpClient;
            _categoryRepository = categoryRepository;
        }

        public async Task<List<AiTransactionResponseDto>> ProcessStatementAsync(Stream fileStream, string fileName, string contentType, Guid accountId, Guid userId)
        {
            var categories = await _categoryRepository.GetAllByUserIdAsync(userId);
            
            var categoryMap = categories.ToDictionary(c => c.Name, c => c.Id.ToString());
            var categoriesJson = JsonSerializer.Serialize(categoryMap);

            using var content = new MultipartFormDataContent();
            
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(fileContent, "file", fileName);

            content.Add(new StringContent(accountId.ToString()), "accountId");
            content.Add(new StringContent(categoriesJson), "categoriesJson");

            var response = await _httpClient.PostAsync("api/ai/process-file", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                var result = JsonSerializer.Deserialize<AiResponseWrapper>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Data ?? new List<AiTransactionResponseDto>();
            }

            throw new Exception($"Falha no processamento da IA: {response.ReasonPhrase}");
        }

        public async Task<ProactiveInsightResponseDto> GetEmergencyReserveInsightAsync(string jwtToken)
        {
            var payload = new { jwt_token = jwtToken };
            var response = await _httpClient.PostAsJsonAsync("api/ai/proactive/emergency-reserve", payload);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<ProactiveInsightPythonResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new ProactiveInsightResponseDto
                {
                    Success = result?.Success ?? false,
                    Message = result?.Erro,
                    Curiosity = result?.Curiosidade,
                    Information = result?.Informacao,
                    Suggestion = result?.Sugestao,
                    HasAdequateReserve = result?.ReservaAdequada ?? false,
                    AlreadyHasReserveGoal = result?.PossuiMetaReserva ?? false,
                    IdealAmount = result?.ValorIdeal ?? 0m,
                    CurrentAmount = result?.ValorAtual ?? 0m,
                    MissingAmount = result?.ValorFaltante ?? 0m,
                    PercentAchieved = result?.PercentualAtingido ?? 0m
                };
            }

            throw new Exception($"Falha ao obter insight da IA: {response.ReasonPhrase}");
        }

        private class AiResponseWrapper
        {
            public bool Success { get; set; }
            public List<AiTransactionResponseDto>? Data { get; set; }
        }

        private class ProactiveInsightPythonResponse
        {
            public bool Success { get; set; }
            public string? Erro { get; set; }

            [JsonPropertyName("curiosidade")]
            public string? Curiosidade { get; set; }

            [JsonPropertyName("informacao")]
            public string? Informacao { get; set; }

            [JsonPropertyName("sugestao")]
            public string? Sugestao { get; set; }

            [JsonPropertyName("reserva_adequada")]
            public bool ReservaAdequada { get; set; }

            [JsonPropertyName("possui_meta_reserva")]
            public bool PossuiMetaReserva { get; set; }

            [JsonPropertyName("valor_ideal")]
            public decimal ValorIdeal { get; set; }

            [JsonPropertyName("valor_atual")]
            public decimal ValorAtual { get; set; }

            [JsonPropertyName("valor_faltante")]
            public decimal ValorFaltante { get; set; }

            [JsonPropertyName("percentual_atingido")]
            public decimal PercentualAtingido { get; set; }
        }
    }
}