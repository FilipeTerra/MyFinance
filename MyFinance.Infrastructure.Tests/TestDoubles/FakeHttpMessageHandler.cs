using System.Net;
using System.Text;

namespace MyFinance.Infrastructure.Tests.TestDoubles;

/// <summary>
/// Handler HTTP falso para testar clients de integração sem tocar a rede.
///
/// A lista <see cref="Requests"/> é o que permite assertar a requisição efetivamente
/// montada — URL, query string e headers — e não só a resposta processada.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _responder;
    private readonly Exception? _toThrow;

    public List<HttpRequestMessage> Requests { get; } = new();

    /// <summary>Sempre responde o mesmo status e corpo.</summary>
    public FakeHttpMessageHandler(HttpStatusCode status, string json)
    {
        _responder = _ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    /// <summary>Responde conforme a requisição — para endpoints múltiplos (ex: as duas séries do BCB).</summary>
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    /// <summary>Simula falha de transporte (rede fora, DNS, timeout).</summary>
    public FakeHttpMessageHandler(Exception toThrow)
    {
        _toThrow = toThrow;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (_toThrow is not null)
            return Task.FromException<HttpResponseMessage>(_toThrow);

        return Task.FromResult(_responder!(request));
    }

    /// <summary>Cria um HttpClient já apontado para uma base, usando este handler.</summary>
    public HttpClient ToHttpClient(string baseUrl) =>
        new(this) { BaseAddress = new Uri(baseUrl) };

    public static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
