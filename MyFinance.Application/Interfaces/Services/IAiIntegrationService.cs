namespace MyFinance.Application.Interfaces.Services
{
    public interface IAiIntegrationService
    {
        Task<string> ProcessStatementAsync(Stream fileStream, string fileName, string contentType, Guid accountId);
    }
}