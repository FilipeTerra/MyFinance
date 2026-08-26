using MyFinance.Application.Dtos;

namespace MyFinance.Infrastructure.Integrations.AiAgent.Contracts
{
    internal sealed class AiResponseWrapper
    {
        public bool Success { get; set; }
        public List<AiTransactionResponseDto>? Data { get; set; }
    }
}
