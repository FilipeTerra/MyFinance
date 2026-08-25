using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IStockMarketIntegrationService
    {
        /// <summary>
        /// Busca a cotação mais recente do ticker. Retorna null quando o ticker
        /// não é encontrado ou a consulta externa falha.
        /// </summary>
        Task<decimal?> GetQuoteAsync(string ticker);

        /// <summary>
        /// Busca o histórico de cotações do ticker nos últimos `meses` meses.
        /// Retorna lista vazia quando o ticker não é encontrado ou a consulta externa falha.
        /// </summary>
        Task<IEnumerable<CotacaoPontoDto>> GetHistoryAsync(string ticker, int meses);

        /// <summary>
        /// Busca a taxa Selic anual vigente (real, via Banco Central). Retorna null
        /// quando a consulta externa falha — o chamador decide o fallback.
        /// </summary>
        Task<decimal?> GetTaxaSelicAsync();
    }
}
