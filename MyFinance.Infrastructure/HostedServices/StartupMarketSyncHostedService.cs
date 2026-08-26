using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Infrastructure.HostedServices
{
    /// <summary>
    /// Sincroniza a cotação de todos os investimentos com Ticker uma única vez, ao iniciar a API.
    /// Roda em background (não bloqueia o boot do Kestrel) e não interrompe o startup em caso de falha.
    /// </summary>
    public class StartupMarketSyncHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StartupMarketSyncHostedService> _logger;

        public StartupMarketSyncHostedService(IServiceScopeFactory scopeFactory, ILogger<StartupMarketSyncHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var marketSyncService = scope.ServiceProvider.GetRequiredService<IMarketSyncService>();
                    await marketSyncService.SyncAllAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha inesperada ao sincronizar cotações de mercado no startup.");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
