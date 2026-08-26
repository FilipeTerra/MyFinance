using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IMarketSyncService
    {
        /// <summary>
        /// Sincroniza a cotação de todos os investimentos com Ticker configurado (todos os usuários).
        /// Disparado uma vez a cada inicialização da API.
        /// </summary>
        Task SyncAllAsync();
    }
}
