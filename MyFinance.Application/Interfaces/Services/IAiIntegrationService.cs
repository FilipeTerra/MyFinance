using MyFinance.Application.Dtos;
using MyFinance.Application.Dtos.StatementImport;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IAiIntegrationService
    {
        /// <summary>
        /// Verifica rapidamente se o agente de IA está no ar. Existe para que a
        /// importação não fique parada no timeout longo do processamento quando o
        /// agente simplesmente não está rodando.
        /// </summary>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Último recurso para arquivos que nenhum parser determinístico reconheceu:
        /// o LLM interpreta o texto bruto e devolve as transações.
        /// Devolve <c>null</c> quando o agente não respondeu — importar sem IA é o
        /// caminho normal, não uma exceção.
        /// </summary>
        Task<IReadOnlyList<ParsedStatementEntry>?> ExtractStatementAsync(StatementFile file);

        /// <summary>
        /// Sugere categoria para as descrições que a cadeia determinística não
        /// resolveu, escolhendo entre as categorias que o usuário já tem.
        /// Devolve <c>null</c> quando o agente não respondeu.
        /// </summary>
        Task<IReadOnlyDictionary<string, string>?> SuggestCategoriesAsync(
            IReadOnlyList<string> descriptions, IReadOnlyList<string> categoryNames);

        Task<ProactiveInsightResponseDto> GetEmergencyReserveInsightAsync(string jwtToken);

        Task<LifestyleInsightResponseDto> GetLifestyleInflationInsightAsync(string jwtToken);
    }
}
