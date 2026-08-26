using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;

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
        /// Busca os indicadores fundamentalistas do ticker. Retorna null quando o
        /// ticker não é encontrado ou a consulta externa falha. Indicadores que o
        /// provedor não disponibiliza vêm como null dentro do DTO — nunca como zero.
        /// </summary>
        Task<IndicadoresFundamentalistasDto?> GetIndicadoresAsync(string ticker);
    }
}
