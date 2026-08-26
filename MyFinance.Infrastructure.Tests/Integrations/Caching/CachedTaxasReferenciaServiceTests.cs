using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyFinance.Infrastructure.Integrations.BancoCentral;
using MyFinance.Infrastructure.Integrations.Caching;
using MyFinance.Infrastructure.Tests.TestDoubles;

namespace MyFinance.Infrastructure.Tests.Integrations.Caching;

public class CachedTaxasReferenciaServiceTests
{
    private const string SelicJson = """[{"data":"16/09/2026","valor":"14.00"}]""";
    private const string IpcaJson = """[{"data":"01/07/2026","valor":"4.44"}]""";

    private static (CachedTaxasReferenciaService Sut, FakeHttpMessageHandler Handler) BuildSut()
    {
        var handler = new FakeHttpMessageHandler(req =>
            req.RequestUri!.ToString().Contains("sgs.432")
                ? FakeHttpMessageHandler.Json(HttpStatusCode.OK, SelicJson)
                : FakeHttpMessageHandler.Json(HttpStatusCode.OK, IpcaJson));

        var client = new BancoCentralRatesClient(
            handler.ToHttpClient("https://api.bcb.gov.br/dados/serie/"),
            Options.Create(new BancoCentralOptions()),
            NullLogger<BancoCentralRatesClient>.Instance);

        var sut = new CachedTaxasReferenciaService(
            client,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new CachingOptions()));

        return (sut, handler);
    }

    [Fact]
    public async Task GetTaxasReferenciaAsync_CalledTwice_HitsBcbOnce()
    {
        var (sut, handler) = BuildSut();

        var primeira = await sut.GetTaxasReferenciaAsync();
        var segunda = await sut.GetTaxasReferenciaAsync();

        // 2 requisições = as duas séries de UMA consulta; a segunda chamada veio do cache.
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(14.00m, primeira!.SelicAnualPct);
        Assert.Same(primeira, segunda);
    }
}
