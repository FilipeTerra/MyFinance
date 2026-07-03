using MyFinance.Application.Dtos;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IAiIntegrationService
    {
        Task<List<AiTransactionResponseDto>> ProcessStatementAsync(Stream fileStream, string fileName, string contentType, Guid accountId, Guid userId);
        Task<ProactiveInsightResponseDto> GetEmergencyReserveInsightAsync(string jwtToken);
        Task<LifestyleInsightResponseDto> GetLifestyleInflationInsightAsync(string jwtToken);
    }
}