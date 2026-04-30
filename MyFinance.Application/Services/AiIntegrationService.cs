using System.Net.Http.Headers;
using System.Text.Json;
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

        private class AiResponseWrapper
        {
            public bool Success { get; set; }
            public List<AiTransactionResponseDto> Data { get; set; }
        }
    }
}