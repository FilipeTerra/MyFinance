using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyFinance.Infrastructure.Integrations.BancoCentral;
using MyFinance.Infrastructure.Tests.TestDoubles;

namespace MyFinance.Infrastructure.Tests.Integrations.BancoCentral;

public class BancoCentralRatesClientTests
{
    private const string SelicJson = """[{"data":"16/09/2026","valor":"14.00"}]""";
    private const string IpcaJson = """[{"data":"01/07/2026","valor":"4.44"}]""";

    private static readonly BancoCentralOptions DefaultOptions = new();

    /// <summary>Responde conforme a série pedida na URL — as duas são buscadas em paralelo.</summary>
    private static FakeHttpMessageHandler BuildHandler(
        HttpStatusCode selicStatus = HttpStatusCode.OK, string selicBody = SelicJson,
        HttpStatusCode ipcaStatus = HttpStatusCode.OK, string ipcaBody = IpcaJson)
    {
        return new FakeHttpMessageHandler(req =>
            req.RequestUri!.ToString().Contains($"sgs.{DefaultOptions.SerieSelicMeta}")
                ? FakeHttpMessageHandler.Json(selicStatus, selicBody)
                : FakeHttpMessageHandler.Json(ipcaStatus, ipcaBody));
    }

    private static BancoCentralRatesClient BuildSut(FakeHttpMessageHandler handler, BancoCentralOptions? options = null) =>
        new(handler.ToHttpClient("https://api.bcb.gov.br/dados/serie/"),
            Options.Create(options ?? new BancoCentralOptions()),
            NullLogger<BancoCentralRatesClient>.Instance);

    [Fact]
    public async Task GetTaxasReferenciaAsync_WithBothSeries_ParsesValuesAndDates()
    {
        var sut = BuildSut(BuildHandler());

        var taxas = await sut.GetTaxasReferenciaAsync();

        Assert.NotNull(taxas);
        // "14.00" precisa virar 14, não 1400 — o BCB usa ponto decimal e o
        // ambiente é pt-BR, onde o ponto seria separador de milhar.
        Assert.Equal(14.00m, taxas!.SelicAnualPct);
        Assert.Equal(4.44m, taxas.IpcaAnualPct);
        Assert.Equal("16/09/2026", taxas.DataReferenciaSelic);
        Assert.Equal("01/07/2026", taxas.DataReferenciaIpca);
        Assert.Contains("tempo real", taxas.Fonte);
    }

    [Fact]
    public async Task GetTaxasReferenciaAsync_ComputesDerivedRates()
    {
        var sut = BuildSut(BuildHandler());

        var taxas = await sut.GetTaxasReferenciaAsync();

        // Valores conferidos contra a implementação Python original.
        Assert.Equal(1.0979m, taxas!.SelicMensalPct, 3);
        Assert.Equal(0.3627m, taxas.IpcaMensalPct, 3);
        Assert.Equal(9.1536m, taxas.JurosRealAnualPct, 3);
        Assert.Equal(13.90m, taxas.CdiAnualPct);
    }

    [Fact]
    public async Task GetTaxasReferenciaAsync_QueriesBothSeries()
    {
        var handler = BuildHandler();
        var sut = BuildSut(handler);

        await sut.GetTaxasReferenciaAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains(handler.Requests, r => r.RequestUri!.ToString().Contains("sgs.432"));
        Assert.Contains(handler.Requests, r => r.RequestUri!.ToString().Contains("sgs.13522"));
    }

    [Fact]
    public async Task GetTaxasReferenciaAsync_WhenOneSeriesFails_FallsBackOnlyThatSeries()
    {
        var sut = BuildSut(BuildHandler(ipcaStatus: HttpStatusCode.InternalServerError, ipcaBody: "erro"));

        var taxas = await sut.GetTaxasReferenciaAsync();

        Assert.Equal(14.00m, taxas!.SelicAnualPct);            // real
        Assert.Equal(4.72m, taxas.IpcaAnualPct);               // fallback
        Assert.Contains("parcial", taxas.Fonte);
    }

    [Fact]
    public async Task GetTaxasReferenciaAsync_WhenBothSeriesFail_UsesFullFallbackAndNeverReturnsNull()
    {
        var sut = BuildSut(new FakeHttpMessageHandler(new HttpRequestException("BCB fora do ar")));

        var taxas = await sut.GetTaxasReferenciaAsync();

        Assert.NotNull(taxas);
        Assert.Equal(14.25m, taxas!.SelicAnualPct);
        Assert.Equal(4.72m, taxas.IpcaAnualPct);
        Assert.Contains("Fallback", taxas.Fonte);
        Assert.Equal("fallback", taxas.DataReferenciaSelic);
    }

    [Fact]
    public async Task GetTaxasReferenciaAsync_WithUnparseableValue_FallsBack()
    {
        var sut = BuildSut(BuildHandler(selicBody: """[{"data":"16/09/2026","valor":"n/d"}]"""));

        var taxas = await sut.GetTaxasReferenciaAsync();

        Assert.Equal(14.25m, taxas!.SelicAnualPct);
    }

    [Fact]
    public async Task GetTaxasReferenciaAsync_WithEmptySeries_FallsBack()
    {
        var sut = BuildSut(BuildHandler(selicBody: "[]"));

        var taxas = await sut.GetTaxasReferenciaAsync();

        Assert.Equal(14.25m, taxas!.SelicAnualPct);
    }
}
