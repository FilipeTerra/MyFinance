using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Infrastructure.Integrations.Brapi;

namespace MyFinance.Infrastructure.Integrations.Caching
{
    /// <summary>
    /// Cache dos dados de mercado, sobreposto ao <see cref="BrapiStockClient"/>.
    ///
    /// Cacheia o <see cref="AcaoSnapshot"/> INTEIRO por ticker e projeta os três
    /// métodos da interface a partir dele. Consequência prática: a sequência
    /// GetHistoryAsync + GetQuoteAsync que o MarketSyncService executa por
    /// investimento passa a custar UMA requisição em vez de duas, e uma consulta
    /// de indicadores logo depois é acerto de cache.
    ///
    /// O TTL curto de falha existe para que um ticker inválido cadastrado não
    /// consuma cota a cada boot da API.
    /// </summary>
    public class CachedStockMarketService : IStockMarketIntegrationService
    {
        private readonly BrapiStockClient _inner;
        private readonly IMemoryCache _cache;
        private readonly CachingOptions _options;

        public CachedStockMarketService(
            BrapiStockClient inner,
            IMemoryCache cache,
            IOptions<CachingOptions> options)
        {
            _inner = inner;
            _cache = cache;
            _options = options.Value;
        }

        public async Task<decimal?> GetQuoteAsync(string ticker)
        {
            var snapshot = await GetSnapshotAsync(ticker, meses: 3);
            return snapshot?.PrecoAtual;
        }

        public async Task<IEnumerable<CotacaoPontoDto>> GetHistoryAsync(string ticker, int meses)
        {
            var snapshot = await GetSnapshotAsync(ticker, meses);
            return snapshot?.Historico ?? Array.Empty<CotacaoPontoDto>();
        }

        public async Task<IndicadoresFundamentalistasDto?> GetIndicadoresAsync(string ticker)
        {
            var snapshot = await GetSnapshotAsync(ticker, meses: 3);
            return snapshot?.Indicadores;
        }

        private async Task<AcaoSnapshot?> GetSnapshotAsync(string ticker, int meses)
        {
            // Chave case-insensitive: "petr4" e "PETR4" são o mesmo ativo.
            var key = $"acao:{ticker.Trim().ToUpperInvariant()}:{meses}";

            if (_cache.TryGetValue(key, out AcaoSnapshot? cached))
                return cached;

            var snapshot = await _inner.GetSnapshotAsync(ticker, meses);

            var ttl = snapshot is null
                ? TimeSpan.FromMinutes(_options.FalhaTtlMinutes)
                : TimeSpan.FromMinutes(_options.AcaoTtlMinutes);

            _cache.Set(key, snapshot, ttl);
            return snapshot;
        }
    }
}
