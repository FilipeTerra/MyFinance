using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Infrastructure.Integrations.AiAgent;
using MyFinance.Infrastructure.Integrations.BancoCentral;
using MyFinance.Infrastructure.Integrations.Brapi;
using MyFinance.Infrastructure.Integrations.Caching;

namespace MyFinance.Infrastructure.Integrations
{
    /// <summary>
    /// Ponto único de registro das integrações com serviços externos.
    /// Concentra aqui o que antes eram URLs hardcoded espalhadas pelo Program.cs.
    /// </summary>
    public static class IntegrationsServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrations(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BrapiOptions>(configuration.GetSection(BrapiOptions.SectionName));
            services.Configure<BancoCentralOptions>(configuration.GetSection(BancoCentralOptions.SectionName));
            services.Configure<AiAgentOptions>(configuration.GetSection(AiAgentOptions.SectionName));
            services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));

            services.AddMemoryCache();

            var brapi = configuration.GetSection(BrapiOptions.SectionName).Get<BrapiOptions>() ?? new BrapiOptions();
            var bcb = configuration.GetSection(BancoCentralOptions.SectionName).Get<BancoCentralOptions>() ?? new BancoCentralOptions();
            var aiAgent = configuration.GetSection(AiAgentOptions.SectionName).Get<AiAgentOptions>() ?? new AiAgentOptions();

            // Clients concretos: falam HTTP e mapeiam a resposta, sem cache.
            services.AddHttpClient<BrapiStockClient>(client =>
            {
                client.BaseAddress = new Uri(brapi.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(brapi.TimeoutSeconds);
            });

            services.AddHttpClient<BancoCentralRatesClient>(client =>
            {
                client.BaseAddress = new Uri(bcb.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(bcb.TimeoutSeconds);
            });

            services.AddHttpClient<IAiIntegrationService, AiAgentClient>(client =>
            {
                client.BaseAddress = new Uri(aiAgent.BaseUrl);
                client.Timeout = TimeSpan.FromMinutes(aiAgent.TimeoutMinutes);
            });

            // As interfaces públicas resolvem para os decorators de cache, que
            // envolvem os clients concretos registrados acima.
            services.AddScoped<IStockMarketIntegrationService, CachedStockMarketService>();
            services.AddScoped<ITaxasReferenciaIntegrationService, CachedTaxasReferenciaService>();

            return services;
        }
    }
}
