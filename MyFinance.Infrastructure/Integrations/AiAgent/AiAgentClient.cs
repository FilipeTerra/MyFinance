using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinance.Application.Dtos;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Infrastructure.Integrations.AiAgent.Contracts;

namespace MyFinance.Infrastructure.Integrations.AiAgent
{
    /// <summary>
    /// Cliente do microsserviço de agentes de IA (FastAPI/LangGraph): extração
    /// semântica de extratos, sugestão de categorias e insights proativos.
    ///
    /// Os insights são fail-fast: sem o agente não existe insight nenhum, e o
    /// controller precisa responder erro. Já a importação de extrato trata o
    /// agente como opcional — os métodos usados por ela devolvem null em vez de
    /// lançar, porque o fluxo determinístico continua valendo sem eles.
    /// </summary>
    public class AiAgentClient : IAiIntegrationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<AiAgentClient> _logger;
        private readonly AiAgentOptions _options;

        public AiAgentClient(
            HttpClient httpClient,
            IOptions<AiAgentOptions> options,
            ILogger<AiAgentClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.HealthTimeoutSeconds));
                var response = await _httpClient.GetAsync("health", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(
                    "Agente de IA indisponível no health check: {Erro}. A importação segue sem ele.", ex.Message);
                return false;
            }
        }

        public async Task<IReadOnlyList<ParsedStatementEntry>?> ExtractStatementAsync(StatementFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(file.Content);
                fileContent.Headers.ContentType = ParseContentType(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                var response = await _httpClient.PostAsync("api/ai/extract-statement", content);
                if (!response.IsSuccessStatusCode)
                {
                    await LogFailureAsync(response, "extrair o extrato");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ExtractStatementResponse>(json, JsonOptions);

                if (result is null || !result.Success)
                {
                    _logger.LogWarning(
                        "Agente de IA não conseguiu extrair o extrato: {Mensagem}", result?.Message ?? "sem detalhe");
                    return null;
                }

                return MapEntries(result.Transactions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao chamar o agente de IA para extrair o extrato.");
                return null;
            }
        }

        public async Task<IReadOnlyDictionary<string, string>?> SuggestCategoriesAsync(
            IReadOnlyList<string> descriptions, IReadOnlyList<string> categoryNames)
        {
            if (descriptions.Count == 0)
                return new Dictionary<string, string>();

            try
            {
                // Timeout curto e próprio: sugerir categoria é um bônus, e não pode
                // segurar a importação pelos minutos que o extrator pode levar.
                using var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(_options.SuggestionTimeoutSeconds));

                var response = await _httpClient.PostAsJsonAsync(
                    "api/ai/suggest-categories",
                    new { descriptions, categories = categoryNames },
                    cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    await LogFailureAsync(response, "sugerir categorias");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                var result = JsonSerializer.Deserialize<SuggestCategoriesResponse>(json, JsonOptions);

                if (result is null || !result.Success || result.Suggestions is null)
                    return null;

                return result.Suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao chamar o agente de IA para sugerir categorias.");
                return null;
            }
        }

        private static List<ParsedStatementEntry> MapEntries(List<ExtractedTransactionContract>? transactions)
        {
            var entries = new List<ParsedStatementEntry>();
            if (transactions is null)
                return entries;

            foreach (var item in transactions)
            {
                if (string.IsNullOrWhiteSpace(item.Description)
                    || !DateTime.TryParse(item.Date, CultureInfo.InvariantCulture,
                           DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date))
                    continue;

                var isCredit = string.Equals(item.Tipo, "receita", StringComparison.OrdinalIgnoreCase);
                var amount = Math.Abs(item.Valor);

                entries.Add(new ParsedStatementEntry(
                    DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
                    item.Description.Trim(),
                    isCredit ? amount : -amount,
                    string.IsNullOrWhiteSpace(item.Categoria) ? null : item.Categoria.Trim()));
            }

            return entries;
        }

        private static MediaTypeHeaderValue ParseContentType(string contentType)
        {
            return MediaTypeHeaderValue.TryParse(contentType, out var parsed)
                ? parsed
                : new MediaTypeHeaderValue("application/octet-stream");
        }

        private async Task LogFailureAsync(HttpResponseMessage response, string acao)
        {
            string corpo;
            try { corpo = await response.Content.ReadAsStringAsync(); }
            catch { corpo = "<corpo ilegível>"; }

            _logger.LogWarning(
                "Agente de IA falhou ao {Acao}: status {Status}. Resposta: {Corpo}",
                acao, (int)response.StatusCode, corpo);
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
