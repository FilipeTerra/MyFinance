using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Infrastructure.Integrations.AiAgent;
using MyFinance.Infrastructure.Tests.TestDoubles;

namespace MyFinance.Infrastructure.Tests.Integrations.AiAgent;

public class AiAgentClientTests
{
    private const string ExtractJson = """
        {"success":true,"transactions":[
            {"date":"2026-08-30","description":"IFD*IFOOD CLUB","valor":5.95,"tipo":"despesa","categoria":"Servicos"},
            {"date":"2026-08-25","description":"PAGAMENTO ON LINE","valor":150.0,"tipo":"receita","categoria":null}
        ]}
        """;

    private static StatementFile Arquivo() =>
        new("extrato.csv", "text/csv", new byte[] { 1, 2, 3 });

    private static AiAgentClient BuildSut(FakeHttpMessageHandler handler) =>
        new(handler.ToHttpClient("http://127.0.0.1:8181/"),
            Options.Create(new AiAgentOptions()),
            NullLogger<AiAgentClient>.Instance);

    // ---------- Health ----------

    [Fact]
    public async Task IsAvailableAsync_QuandoOAgenteResponde_RetornaVerdadeiro()
    {
        var sut = BuildSut(new FakeHttpMessageHandler(HttpStatusCode.OK, """{"status":"ok"}"""));

        Assert.True(await sut.IsAvailableAsync());
    }

    [Fact]
    public async Task IsAvailableAsync_QuandoOAgenteEstaFora_RetornaFalsoSemLancar()
    {
        var sut = BuildSut(new FakeHttpMessageHandler(new HttpRequestException("connection refused")));

        Assert.False(await sut.IsAvailableAsync());
    }

    // ---------- Extração ----------

    [Fact]
    public async Task ExtractStatementAsync_ConverteTipoEmSinalDoValor()
    {
        var sut = BuildSut(new FakeHttpMessageHandler(HttpStatusCode.OK, ExtractJson));

        var entries = await sut.ExtractStatementAsync(Arquivo());

        Assert.NotNull(entries);
        Assert.Equal(2, entries!.Count);
        Assert.Equal(-5.95m, entries[0].Amount);
        Assert.Equal("Servicos", entries[0].FileCategoryName);
        Assert.Equal(150.0m, entries[1].Amount);
        Assert.Null(entries[1].FileCategoryName);
    }

    [Fact]
    public async Task ExtractStatementAsync_QuandoOAgenteEstaFora_RetornaNullEmVezDeLancar()
    {
        // Importar sem IA é o caminho normal, não uma exceção: quem chama precisa
        // conseguir seguir em frente.
        var sut = BuildSut(new FakeHttpMessageHandler(new HttpRequestException("connection refused")));

        Assert.Null(await sut.ExtractStatementAsync(Arquivo()));
    }

    [Fact]
    public async Task ExtractStatementAsync_QuandoORetornoNaoTemSucesso_RetornaNull()
    {
        var sut = BuildSut(new FakeHttpMessageHandler(
            HttpStatusCode.OK, """{"success":false,"message":"modelo indisponível"}"""));

        Assert.Null(await sut.ExtractStatementAsync(Arquivo()));
    }

    [Fact]
    public async Task ExtractStatementAsync_DescartaLinhaSemDataOuDescricao()
    {
        var sut = BuildSut(new FakeHttpMessageHandler(HttpStatusCode.OK, """
            {"success":true,"transactions":[
                {"date":"data inválida","description":"X","valor":1.0,"tipo":"despesa"},
                {"date":"2026-08-30","description":"","valor":1.0,"tipo":"despesa"},
                {"date":"2026-08-30","description":"OK","valor":1.0,"tipo":"despesa"}
            ]}
            """));

        var entries = await sut.ExtractStatementAsync(Arquivo());

        Assert.Equal("OK", Assert.Single(entries!).Description);
    }

    // ---------- Sugestão de categorias ----------

    [Fact]
    public async Task SuggestCategoriesAsync_DevolveOMapaDeSugestoes()
    {
        var sut = BuildSut(new FakeHttpMessageHandler(
            HttpStatusCode.OK, """{"success":true,"suggestions":{"POSTO X":"Transporte"}}"""));

        var suggestions = await sut.SuggestCategoriesAsync(new[] { "POSTO X" }, new[] { "Transporte" });

        Assert.Equal("Transporte", Assert.Single(suggestions!).Value);
    }

    [Fact]
    public async Task SuggestCategoriesAsync_QuandoOAgenteEstaFora_RetornaNullEmVezDeLancar()
    {
        var sut = BuildSut(new FakeHttpMessageHandler(new HttpRequestException("connection refused")));

        Assert.Null(await sut.SuggestCategoriesAsync(new[] { "POSTO X" }, new[] { "Transporte" }));
    }

    [Fact]
    public async Task SuggestCategoriesAsync_SemDescricoes_NaoChamaOAgente()
    {
        var handler = new FakeHttpMessageHandler(new HttpRequestException("não deveria ser chamado"));
        var sut = BuildSut(handler);

        var suggestions = await sut.SuggestCategoriesAsync(Array.Empty<string>(), new[] { "Transporte" });

        Assert.Empty(suggestions!);
        Assert.Empty(handler.Requests);
    }
}
