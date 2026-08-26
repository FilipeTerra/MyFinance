using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Infrastructure.Integrations.BancoCentral;

namespace MyFinance.Infrastructure.Integrations.Caching
{
    /// <summary>
    /// Cache das taxas de referência. A Selic muda em reuniões do Copom (~45 dias)
    /// e o IPCA é mensal — cachear por horas não perde informação alguma e evita
    /// bater no BCB a cada projeção calculada ou pergunta feita ao agente.
    /// </summary>
    public class CachedTaxasReferenciaService : ITaxasReferenciaIntegrationService
    {
        private const string CacheKey = "taxas-referencia";

        private readonly BancoCentralRatesClient _inner;
        private readonly IMemoryCache _cache;
        private readonly CachingOptions _options;

        public CachedTaxasReferenciaService(
            BancoCentralRatesClient inner,
            IMemoryCache cache,
            IOptions<CachingOptions> options)
        {
            _inner = inner;
            _cache = cache;
            _options = options.Value;
        }

        public async Task<TaxasReferenciaDto?> GetTaxasReferenciaAsync()
        {
            if (_cache.TryGetValue(CacheKey, out TaxasReferenciaDto? cached))
                return cached;

            var resultado = await _inner.GetTaxasReferenciaAsync();

            // Falha recebe TTL curto para não congelar um erro transitório por horas.
            var ttl = resultado is null
                ? TimeSpan.FromMinutes(_options.FalhaTtlMinutes)
                : TimeSpan.FromHours(_options.TaxasReferenciaTtlHours);

            _cache.Set(CacheKey, resultado, ttl);
            return resultado;
        }
    }
}
