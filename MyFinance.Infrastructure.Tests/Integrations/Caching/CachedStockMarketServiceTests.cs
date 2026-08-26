using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyFinance.Infrastructure.Integrations.Brapi;
using MyFinance.Infrastructure.Integrations.Caching;
using MyFinance.Infrastructure.Tests.TestDoubles;

namespace MyFinance.Infrastructure.Tests.Integrations.Caching;

/// <summary>
/// Usa o client real sobre um handler falso e conta as requisições HTTP — assim o
/// teste mede a economia de chamadas de verdade, em vez de um mock combinado.
/// </summary>
public class CachedStockMarketServiceTests
{
    private static readonly string PetrFixture =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "brapi-petr4.json"));

    private static (CachedStockMarketService Sut, FakeHttpMessageHandler Handler) BuildSut(
        HttpStatusCode status = HttpStatusCode.OK, string? body = null)
    {
        var handler = new FakeHttpMessageHandler(status, body ?? PetrFixture);
        var client = new BrapiStockClient(
            handler.ToHttpClient("https://brapi.dev/api/"),
            Options.Create(new BrapiOptions { Token = "t" }),
            NullLogger<BrapiStockClient>.Instance);

        var sut = new CachedStockMarketService(
            client,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new CachingOptions()));

        return (sut, handler);
    }

    [Fact]
    public async Task GetQuoteAsync_CalledTwice_HitsProviderOnce()
    {
        var (sut, handler) = BuildSut();

        await sut.GetQuoteAsync("PETR4");
        await sut.GetQuoteAsync("PETR4");

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task HistoryThenQuoteThenIndicadores_ForSameTicker_HitsProviderOnce()
    {
        // Esta é a economia central do desenho: o MarketSyncService pede histórico
        // e cotação por investimento, e o agente pode pedir indicadores logo depois.
        var (sut, handler) = BuildSut();

        var historico = await sut.GetHistoryAsync("PETR4", 3);
        var cotacao = await sut.GetQuoteAsync("PETR4");
        var indicadores = await sut.GetIndicadoresAsync("PETR4");

        Assert.Single(handler.Requests);
        Assert.NotEmpty(historico);
        Assert.Equal(41.52m, cotacao);
        Assert.Equal("PETR4", indicadores!.Ticker);
    }

    [Fact]
    public async Task GetQuoteAsync_IsCaseInsensitive()
    {
        var (sut, handler) = BuildSut();

        await sut.GetQuoteAsync("petr4");
        await sut.GetQuoteAsync("PETR4");
        await sut.GetQuoteAsync("  Petr4 ");

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetQuoteAsync_ForDifferentTickers_UsesSeparateEntries()
    {
        var (sut, handler) = BuildSut();

        await sut.GetQuoteAsync("PETR4");
        await sut.GetQuoteAsync("VALE3");

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetQuoteAsync_WhenProviderFails_CachesTheFailure()
    {
        // Sem cache negativo, um ticker inválido cadastrado consumiria cota da API
        // a cada boot, para sempre.
        var (sut, handler) = BuildSut(HttpStatusCode.NotFound, "{}");

        Assert.Null(await sut.GetQuoteAsync("XXXX9"));
        Assert.Null(await sut.GetQuoteAsync("XXXX9"));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenProviderFails_ReturnsEmptyNotNull()
    {
        var (sut, _) = BuildSut(HttpStatusCode.InternalServerError, "erro");

        Assert.Empty(await sut.GetHistoryAsync("PETR4", 3));
    }
}
