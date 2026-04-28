using MyFinance.Application.Interfaces.Services;
using System.Net.Http.Headers;

namespace MyFinance.Application.Services
{
    public class AiIntegrationService : IAiIntegrationService
    {
        private readonly HttpClient _httpClient;

        public AiIntegrationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> ProcessStatementAsync(Stream fileStream, string fileName, string contentType, Guid accountId)
        {
            using var content = new MultipartFormDataContent();
            
            var fileContent = new StreamContent(fileStream);
            
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(accountId.ToString()), "accountId");

            var response = await _httpClient.PostAsync("api/ai/process-file", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new Exception($"Erro na API de IA. StatusCode: {response.StatusCode}");
        }
    }
}