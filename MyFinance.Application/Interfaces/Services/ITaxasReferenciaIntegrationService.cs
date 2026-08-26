using System.Threading.Tasks;
using MyFinance.Application.Dtos.Mercado;

namespace MyFinance.Application.Interfaces.Services
{
    public interface ITaxasReferenciaIntegrationService
    {
        /// <summary>
        /// Busca as taxas de referência da economia brasileira (Selic, IPCA, CDI,
        /// juros reais). Retorna null apenas em falha irrecuperável — a implementação
        /// aplica valores de fallback quando a consulta externa falha.
        /// </summary>
        Task<TaxasReferenciaDto?> GetTaxasReferenciaAsync();
    }
}
