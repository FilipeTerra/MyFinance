using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyFinance.Infrastructure.Integrations.Brapi;
using MyFinance.Infrastructure.Tests.TestDoubles;

namespace MyFinance.Infrastructure.Tests.Integrations.Brapi;

public class BrapiStockClientTests
{
    private static readonly string PetrFixture =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "brapi-petr4.json"));

    private static BrapiStockClient BuildSut(FakeHttpMessageHandler handler, BrapiOptions? options = null) =>
        new(handler.ToHttpClient("https://brapi.dev/api/"),
            Options.Create(options ?? new BrapiOptions { Token = "tok-123" }),
            NullLogger<BrapiStockClient>.Instance);

    [Fact]
    public async Task GetSnapshotAsync_WithRealPayload_MapsAllIndicators()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PetrFixture);
        var sut = BuildSut(handler);

        var snapshot = await sut.GetSnapshotAsync("PETR4", 3);

        Assert.NotNull(snapshot);
        var i = snapshot!.Indicadores;
        Assert.Equal("PETR4", i.Ticker);
        Assert.Equal(41.52m, i.PrecoAtualBrl);
        Assert.Equal(29.31m, i.Minima52Semanas);
        Assert.Equal(50.69m, i.Maxima52Semanas);
        Assert.Equal(4.44m, i.PL);
        Assert.Equal(4.23m, i.EvEbitda);
        Assert.Equal(676.28m, i.DividaBilhoes);
        Assert.Equal(85.80m, i.FluxoCaixaLivreBilhoes);
        Assert.Equal(49.83m, i.MargemEbitda);
        Assert.Equal(11.23m, i.CrescimentoReceita);
        Assert.Equal(27.81m, i.ReturnOnEquity);
        Assert.Equal(24.39m, i.MargemLucro);
    }

    [Fact]
    public async Task GetSnapshotAsync_ConvertsDividendYieldFractionToPercent()
    {
        // O brapi entrega o DY como fração (0.09), ao contrário do que o yfinance fazia.
        // Errar essa escala faria o agente aconselhar com um número 100x menor.
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PetrFixture);
        var sut = BuildSut(handler);

        var snapshot = await sut.GetSnapshotAsync("PETR4", 3);

        Assert.Equal(9.00m, snapshot!.Indicadores.DividendYield);
    }

    [Fact]
    public async Task GetSnapshotAsync_FieldsAbsentFromFreePlan_AreNullNotZero()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PetrFixture);
        var sut = BuildSut(handler);

        var snapshot = await sut.GetSnapshotAsync("PETR4", 3);

        Assert.Null(snapshot!.Indicadores.Payout);
        Assert.Null(snapshot.Indicadores.DividendYieldMedio5Anos);
    }

    [Fact]
    public async Task GetSnapshotAsync_BuildsUrlWithoutSaSuffixAndWithModules()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PetrFixture);
        var sut = BuildSut(handler);

        await sut.GetSnapshotAsync("petr4", 3);

        var url = handler.Requests.Single().RequestUri!.ToString();
        Assert.Contains("quote/PETR4", url);
        Assert.DoesNotContain(".SA", url);
        Assert.Contains("range=3mo", url);
        Assert.Contains("interval=1d", url);
        Assert.Contains("modules=defaultKeyStatistics,financialData", url);
        Assert.Contains("token=tok-123", url);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithoutToken_OmitsTokenFromUrl()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PetrFixture);
        var sut = BuildSut(handler, new BrapiOptions { Token = "" });

        await sut.GetSnapshotAsync("PETR4", 3);

        Assert.DoesNotContain("token=", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task GetSnapshotAsync_ClampsRequestedMonthsToPlanCeiling()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PetrFixture);
        var sut = BuildSut(handler, new BrapiOptions { Token = "t", MaxHistoryMonths = 3 });

        await sut.GetSnapshotAsync("PETR4", 12);

        Assert.Contains("range=3mo", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task GetSnapshotAsync_UsesAdjustedCloseAndConvertsUnixSeconds()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PetrFixture);
        var sut = BuildSut(handler);

        var snapshot = await sut.GetSnapshotAsync("PETR4", 3);

        var primeiro = snapshot!.Historico.First();
        // adjustedClose 40.799 (e não close 42.51) mantém a série contínua em proventos.
        Assert.Equal(40.80m, primeiro.Valor);
        Assert.Equal(new DateTime(2026, 5, 28), primeiro.Data);
        Assert.Equal(DateTimeKind.Unspecified, primeiro.Data.Kind);
        Assert.Equal(3, snapshot.Historico.Count);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithEmptyResults_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"results\":[]}");
        var sut = BuildSut(handler);

        Assert.Null(await sut.GetSnapshotAsync("XXXX9", 3));
    }

    [Fact]
    public async Task GetSnapshotAsync_WithUnauthorized_ReturnsNullWithoutThrowing()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.Unauthorized,
            "{\"error\":true,\"message\":\"Token não fornecido\",\"code\":\"MISSING_TOKEN\"}");
        var sut = BuildSut(handler);

        Assert.Null(await sut.GetSnapshotAsync("WEGE3", 3));
    }

    [Fact]
    public async Task GetSnapshotAsync_WithRateLimitExceeded_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.TooManyRequests, "{\"error\":true}");
        var sut = BuildSut(handler);

        Assert.Null(await sut.GetSnapshotAsync("PETR4", 3));
    }

    [Fact]
    public async Task GetSnapshotAsync_WithNetworkFailure_ReturnsNullWithoutThrowing()
    {
        var handler = new FakeHttpMessageHandler(new HttpRequestException("conexão recusada"));
        var sut = BuildSut(handler);

        Assert.Null(await sut.GetSnapshotAsync("PETR4", 3));
    }

    [Fact]
    public async Task GetSnapshotAsync_WithMalformedJson_ReturnsNullWithoutThrowing()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{");
        var sut = BuildSut(handler);

        Assert.Null(await sut.GetSnapshotAsync("PETR4", 3));
    }

    [Fact]
    public async Task GetSnapshotAsync_WithMissingModules_LeavesIndicatorsNullButKeepsRootFields()
    {
        // Bancos, por exemplo, não trazem alguns módulos — os campos devem ficar
        // null em vez de 0.0, que o agente leria como "margem EBITDA de 0%".
        const string json = """
        {"results":[{"symbol":"ITUB4","regularMarketPrice":39.48,
                     "fiftyTwoWeekLow":35.58,"fiftyTwoWeekHigh":49.67,
                     "historicalDataPrice":[]}]}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var sut = BuildSut(handler);

        var snapshot = await sut.GetSnapshotAsync("ITUB4", 3);

        Assert.NotNull(snapshot);
        Assert.Equal(39.48m, snapshot!.Indicadores.PrecoAtualBrl);
        Assert.Null(snapshot.Indicadores.MargemEbitda);
        Assert.Null(snapshot.Indicadores.EvEbitda);
        Assert.Null(snapshot.Indicadores.FluxoCaixaLivreBilhoes);
        Assert.Empty(snapshot.Historico);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithExplicitNullField_KeepsNull()
    {
        const string json = """
        {"results":[{"symbol":"PETR4","regularMarketPrice":41.52,
                     "defaultKeyStatistics":{"trailingPE":null,"dividendYield":0.09},
                     "historicalDataPrice":[]}]}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var sut = BuildSut(handler);

        var snapshot = await sut.GetSnapshotAsync("PETR4", 3);

        Assert.Null(snapshot!.Indicadores.PL);
        Assert.Equal(9.00m, snapshot.Indicadores.DividendYield);
    }
}
