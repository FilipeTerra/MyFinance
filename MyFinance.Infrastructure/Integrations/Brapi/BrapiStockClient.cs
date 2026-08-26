using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Infrastructure.Integrations.Brapi.Contracts;

namespace MyFinance.Infrastructure.Integrations.Brapi
{
    /// <summary>
    /// Cliente do brapi.dev — dados de mercado da B3.
    ///
    /// Responsabilidade única: fazer a chamada HTTP e mapear a resposta. Não tem
    /// cache (isso é do decorator) nem lógica de negócio. Contrato fail-soft: erros
    /// de rede, HTTP e JSON viram null com log, nunca exceção — o chamador decide
    /// o fallback.
    /// </summary>
    public class BrapiStockClient
    {
        private const string ModulesQuery = "&modules=defaultKeyStatistics,financialData";

        // Descoberto em produção com um token real de plano gratuito: cotação e
        // histórico funcionam para qualquer ticker, mas os módulos fundamentalistas
        // (defaultKeyStatistics/financialData) exigem o plano Pro — só os tickers de
        // demonstração do brapi (PETR4, VALE3, ITUB4, MGLU3) os liberam de graça.
        // Uma vez descoberto que o plano não os inclui, paramos de pedi-los: sem essa
        // memória, todo ticker fora da demo custaria 2 requisições em vez de 1,
        // contra uma cota de 15.000/mês.
        private static volatile bool _modulesConhecidosIndisponiveis;

        /// <summary>Uso exclusivo de testes: garante isolamento entre casos que manipulam o cache estático.</summary>
        internal static void ResetPlanCapabilityCacheForTests() => _modulesConhecidosIndisponiveis = false;

        private readonly HttpClient _httpClient;
        private readonly BrapiOptions _options;
        private readonly ILogger<BrapiStockClient> _logger;

        public BrapiStockClient(
            HttpClient httpClient,
            IOptions<BrapiOptions> options,
            ILogger<BrapiStockClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Busca cotação, histórico e indicadores de um ticker numa única requisição
        /// (quando o plano permite). Retorna null quando o ticker não existe ou a
        /// consulta falha; degrada para cotação+histórico sem indicadores quando o
        /// plano não inclui os módulos fundamentalistas, em vez de falhar por inteiro.
        /// </summary>
        public Task<AcaoSnapshot?> GetSnapshotAsync(string ticker, int meses, CancellationToken ct = default) =>
            GetSnapshotAsync(ticker, meses, comModulos: !_modulesConhecidosIndisponiveis, ct);

        private async Task<AcaoSnapshot?> GetSnapshotAsync(
            string ticker, int meses, bool comModulos, CancellationToken ct)
        {
            var normalizado = ticker.Trim().ToUpperInvariant();
            var (range, clamped) = BrapiRangeMapper.ToRange(meses, _options.MaxHistoryMonths);

            if (clamped)
            {
                _logger.LogWarning(
                    "Histórico de {Meses} meses pedido para {Ticker}, mas o plano do brapi permite no máximo {Max}; usando {Range}.",
                    meses, normalizado, _options.MaxHistoryMonths, range);
            }

            var url = $"quote/{normalizado}?range={range}&interval=1d";
            if (comModulos)
                url += ModulesQuery;
            if (!string.IsNullOrWhiteSpace(_options.Token))
                url += $"&token={_options.Token}";

            try
            {
                var response = await _httpClient.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    if (comModulos && await IsModulesNotAvailableAsync(response, ct))
                    {
                        _modulesConhecidosIndisponiveis = true;
                        _logger.LogWarning(
                            "brapi: módulos de indicadores fundamentalistas exigem plano pago (ticker {Ticker}). " +
                            "Cotação e histórico continuam disponíveis; indicadores ficarão vazios até upgrade do plano.",
                            normalizado);
                        return await GetSnapshotAsync(ticker, meses, comModulos: false, ct);
                    }

                    await LogFalhaHttpAsync(response, normalizado, ct);
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<BrapiQuoteResponse>(ct);
                var result = payload?.Results?.FirstOrDefault();
                if (result is null)
                {
                    _logger.LogWarning("brapi não retornou dados para o ticker {Ticker}.", normalizado);
                    return null;
                }

                return MapToSnapshot(normalizado, result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao consultar o ticker {Ticker} no brapi.", normalizado);
                return null;
            }
        }

        private static async Task<bool> IsModulesNotAvailableAsync(HttpResponseMessage response, CancellationToken ct)
        {
            if (response.StatusCode != HttpStatusCode.Forbidden)
                return false;

            try
            {
                var corpo = await response.Content.ReadFromJsonAsync<BrapiQuoteResponse>(ct);
                return corpo?.Code == "MODULES_NOT_AVAILABLE";
            }
            catch
            {
                return false;
            }
        }

        private async Task LogFalhaHttpAsync(HttpResponseMessage response, string ticker, CancellationToken ct)
        {
            var status = (int)response.StatusCode;

            // O brapi responde 401 tanto para "token ausente" quanto para ticker fora
            // do plano gratuito — distinguir do "ticker inexistente" evita horas de
            // depuração procurando um erro de digitação que não existe.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var corpo = await SafeReadAsync(response, ct);
                var semToken = string.IsNullOrWhiteSpace(_options.Token);
                _logger.LogWarning(
                    "brapi negou acesso ao ticker {Ticker} (401). {Causa} Resposta: {Corpo}",
                    ticker,
                    semToken
                        ? "Nenhum token configurado — apenas PETR4, VALE3, ITUB4 e MGLU3 são gratuitos. Configure ExternalServices:Brapi:Token."
                        : "O token configurado não cobre este ativo ou é inválido.",
                    corpo);
                return;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning(
                    "Cota do brapi excedida (429) ao consultar {Ticker}. O plano gratuito permite 15.000 requisições/mês.",
                    ticker);
                return;
            }

            _logger.LogWarning("brapi respondeu {Status} ao consultar o ticker {Ticker}.", status, ticker);
        }

        private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try { return await response.Content.ReadAsStringAsync(ct); }
            catch { return "<corpo ilegível>"; }
        }

        private static AcaoSnapshot MapToSnapshot(string ticker, BrapiQuoteResult r)
        {
            var ks = r.DefaultKeyStatistics;
            var fd = r.FinancialData;

            var historico = (r.HistoricalDataPrice ?? new List<BrapiHistoricalPoint>())
                // adjustedClose mantém a série contínua em splits/proventos; close é o
                // fallback para os raros pontos em que o provedor não traz o ajustado.
                .Select(p => new { p.Date, Valor = p.AdjustedClose ?? p.Close })
                .Where(p => p.Valor.HasValue)
                .Select(p => new CotacaoPontoDto
                {
                    // Unspecified para casar com o modo legacy de timestamp do Npgsql.
                    Data = DateTime.SpecifyKind(
                        DateTimeOffset.FromUnixTimeSeconds(p.Date).UtcDateTime.Date,
                        DateTimeKind.Unspecified),
                    Valor = Math.Round(p.Valor!.Value, 2)
                })
                .ToList();

            return new AcaoSnapshot
            {
                Ticker = ticker,
                PrecoAtual = Arredondar(r.RegularMarketPrice),
                Historico = historico,
                Indicadores = new IndicadoresFundamentalistasDto
                {
                    Ticker = ticker,
                    PrecoAtualBrl = Arredondar(r.RegularMarketPrice),
                    Minima52Semanas = Arredondar(r.FiftyTwoWeekLow),
                    Maxima52Semanas = Arredondar(r.FiftyTwoWeekHigh),
                    // O brapi entrega o DY como fração (0.09 = 9%), ao contrário dos
                    // demais campos de mesma natureza — daí o ×100.
                    DividendYield = Arredondar(ks?.DividendYield * 100),
                    // Ausentes no plano gratuito (dividendos exigem plano pago).
                    DividendYieldMedio5Anos = null,
                    Payout = null,
                    DividaBilhoes = Arredondar(fd?.TotalDebt / 1_000_000_000m),
                    PL = Arredondar(ks?.TrailingPE),
                    MargemEbitda = Arredondar(fd?.EbitdaMargins * 100),
                    EvEbitda = Arredondar(ks?.EnterpriseToEbitda),
                    CrescimentoReceita = Arredondar(fd?.RevenueGrowth * 100),
                    FluxoCaixaLivreBilhoes = Arredondar(fd?.FreeCashflow / 1_000_000_000m),
                    ReturnOnEquity = Arredondar(fd?.ReturnOnEquity * 100),
                    MargemLucro = Arredondar(fd?.ProfitMargins * 100 ?? ks?.ProfitMargins * 100)
                }
            };
        }

        /// <summary>Arredonda preservando null — ausência de dado nunca vira zero.</summary>
        private static decimal? Arredondar(decimal? valor) =>
            valor.HasValue ? Math.Round(valor.Value, 2) : null;
    }
}
