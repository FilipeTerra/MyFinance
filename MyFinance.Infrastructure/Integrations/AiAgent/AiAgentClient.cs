using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Infrastructure.Integrations.AiAgent.Contracts;

namespace MyFinance.Infrastructure.Integrations.AiAgent
{
    /// <summary>
    /// Cliente do microsserviço de agentes de IA (FastAPI/LangGraph): processamento
    /// semântico de extratos e insights proativos.
    ///
    /// Diferente das demais integrações, este cliente é fail-fast: uma falha aqui
    /// significa que a ação pedida pelo usuário (importar extrato, ver insight) não
    /// aconteceu, e o controller precisa saber disso para responder um erro.
    /// </summary>
    public class AiAgentClient : IAiIntegrationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<AiAgentClient> _logger;

        public AiAgentClient(
            HttpClient httpClient,
            ICategoryRepository categoryRepository,
            ILogger<AiAgentClient> logger)
        {
            _httpClient = httpClient;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<List<AiTransactionResponseDto>> ProcessStatementAsync(
            Stream fileStream, string fileName, string contentType, Guid accountId, Guid userId)
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
            if (!response.IsSuccessStatusCode)
                throw await BuildFailureAsync(response, "processar o extrato");

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AiResponseWrapper>(json, JsonOptions);
            return result?.Data ?? new List<AiTransactionResponseDto>();
        }

        public async Task<ProactiveInsightResponseDto> GetEmergencyReserveInsightAsync(string jwtToken)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/ai/proactive/emergency-reserve", new { jwt_token = jwtToken });

            if (!response.IsSuccessStatusCode)
                throw await BuildFailureAsync(response, "obter o insight de reserva de emergência");

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProactiveInsightPythonResponse>(json, JsonOptions);

            return new ProactiveInsightResponseDto
            {
                Success = result?.Success ?? false,
                Message = result?.Erro,
                ShowCard = result?.ExibirCard ?? false,
                CardType = result?.TipoCard,
                Curiosity = result?.Curiosidade,
                Information = result?.Informacao,
                Suggestion = result?.Sugestao,
                IdealAmount = result?.ValorIdeal ?? 0m,
                CurrentAmount = result?.ValorAtual ?? 0m,
                MissingAmount = result?.ValorFaltante ?? 0m,
                PercentAchieved = result?.PercentualAtingido ?? 0m
            };
        }

        public async Task<LifestyleInsightResponseDto> GetLifestyleInflationInsightAsync(string jwtToken)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/ai/proactive/lifestyle-inflation", new { jwt_token = jwtToken });

            if (!response.IsSuccessStatusCode)
                throw await BuildFailureAsync(response, "obter o insight de inflação de estilo de vida");

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LifestyleInsightPythonResponse>(json, JsonOptions);

            return new LifestyleInsightResponseDto
            {
                Success = result?.Success ?? false,
                Message = result?.Erro,
                Alert = result?.Alerta ?? false,
                Curiosity = result?.Curiosidade,
                Information = result?.Informacao,
                Suggestion = result?.Sugestao,
                LifestylePercentOfIncome = result?.PercentualRendaEstiloVida,
                LifestyleGrowthPercent = result?.VariacaoEstiloVidaPct,
                InvestmentGrowthPercent = result?.VariacaoAportesPct
            };
        }

        private async Task<AiAgentException> BuildFailureAsync(HttpResponseMessage response, string acao)
        {
            var status = (int)response.StatusCode;
            string corpo;
            try { corpo = await response.Content.ReadAsStringAsync(); }
            catch { corpo = "<corpo ilegível>"; }

            _logger.LogError(
                "Agente de IA falhou ao {Acao}: status {Status}. Resposta: {Corpo}",
                acao, status, corpo);

            return new AiAgentException($"Falha ao {acao} no Agente de IA (status {status}).");
        }
    }

    /// <summary>Falha na comunicação com o microsserviço de agentes de IA.</summary>
    public class AiAgentException : Exception
    {
        public AiAgentException(string message) : base(message) { }
    }
}
