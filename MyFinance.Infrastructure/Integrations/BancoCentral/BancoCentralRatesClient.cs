using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Infrastructure.Integrations.BancoCentral.Contracts;

namespace MyFinance.Infrastructure.Integrations.BancoCentral
{
    /// <summary>
    /// Consulta as séries do SGS/BCB (Selic meta e IPCA 12 meses) e deriva as
    /// demais taxas de referência.
    ///
    /// Contrato de falha: este cliente NUNCA retorna null. Se uma das séries
    /// falhar, aplica o valor de fallback configurado apenas para ela e sinaliza
    /// isso em <see cref="TaxasReferenciaDto.Fonte"/> — uma taxa aproximada é
    /// mais útil ao usuário do que um erro, desde que a origem seja explícita.
    /// </summary>
    public class BancoCentralRatesClient : ITaxasReferenciaIntegrationService
    {
        private const string FonteTempoReal = "Banco Central do Brasil (API SGS em tempo real)";
        private const string FonteFallback = "Banco Central do Brasil (Fallback Estático)";
        private const string FonteParcial = "Banco Central do Brasil (parcial — uma série em fallback)";

        private readonly HttpClient _httpClient;
        private readonly BancoCentralOptions _options;
        private readonly ILogger<BancoCentralRatesClient> _logger;

        public BancoCentralRatesClient(
            HttpClient httpClient,
            IOptions<BancoCentralOptions> options,
            ILogger<BancoCentralRatesClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<TaxasReferenciaDto?> GetTaxasReferenciaAsync()
        {
            // As duas séries são independentes — buscar em paralelo corta a latência pela metade.
            var selicTask = BuscarUltimoPontoAsync(_options.SerieSelicMeta, "Selic");
            var ipcaTask = BuscarUltimoPontoAsync(_options.SerieIpca12Meses, "IPCA");
            await Task.WhenAll(selicTask, ipcaTask);

            var selicPonto = selicTask.Result;
            var ipcaPonto = ipcaTask.Result;

            var selicAnual = selicPonto?.Valor ?? _options.FallbackSelicAnualPct;
            var ipcaAnual = ipcaPonto?.Valor ?? _options.FallbackIpcaAnualPct;

            var fonte = (selicPonto, ipcaPonto) switch
            {
                (not null, not null) => FonteTempoReal,
                (null, null) => FonteFallback,
                _ => FonteParcial
            };

            return new TaxasReferenciaDto
            {
                SelicAnualPct = Math.Round(selicAnual, 2),
                SelicMensalPct = Math.Round(ToTaxaMensalEquivalente(selicAnual), 4),
                IpcaAnualPct = Math.Round(ipcaAnual, 2),
                IpcaMensalPct = Math.Round(ToTaxaMensalEquivalente(ipcaAnual), 4),
                JurosRealAnualPct = Math.Round(ToJurosReal(selicAnual, ipcaAnual), 4),
                CdiAnualPct = Math.Round(selicAnual - _options.SpreadCdiPp, 2),
                DataReferenciaSelic = selicPonto?.Data ?? "fallback",
                DataReferenciaIpca = ipcaPonto?.Data ?? "fallback",
                Fonte = fonte
            };
        }

        /// <summary>Converte taxa anual em mensal equivalente: (1+i)^(1/12) - 1.</summary>
        private static decimal ToTaxaMensalEquivalente(decimal taxaAnualPct) =>
            ((decimal)Math.Pow((double)(1 + taxaAnualPct / 100), 1.0 / 12) - 1) * 100;

        /// <summary>Juros reais pela equação de Fisher: (1+selic)/(1+ipca) - 1.</summary>
        private static decimal ToJurosReal(decimal selicAnualPct, decimal ipcaAnualPct) =>
            ((1 + selicAnualPct / 100) / (1 + ipcaAnualPct / 100) - 1) * 100;

        private async Task<(decimal Valor, string Data)?> BuscarUltimoPontoAsync(int serie, string nomeSerie)
        {
            try
            {
                var url = $"bcdata.sgs.{serie}/dados/ultimos/1?formato=json";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Série {Serie} ({Nome}) do BCB respondeu {Status}; usando fallback.",
                        serie, nomeSerie, (int)response.StatusCode);
                    return null;
                }

                var pontos = await response.Content.ReadFromJsonAsync<List<SgsSeriePointResponse>>();
                var ponto = pontos?.FirstOrDefault();
                if (ponto?.Valor is null)
                {
                    _logger.LogWarning("Série {Serie} ({Nome}) do BCB veio vazia; usando fallback.", serie, nomeSerie);
                    return null;
                }

                // O BCB devolve o valor como string com ponto decimal ("14.25").
                // InvariantCulture é obrigatório: em pt-BR o ponto seria separador de milhar.
                if (!decimal.TryParse(ponto.Valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var valor))
                {
                    _logger.LogWarning(
                        "Valor inesperado \"{Valor}\" na série {Serie} ({Nome}); usando fallback.",
                        ponto.Valor, serie, nomeSerie);
                    return null;
                }

                return (valor, ponto.Data ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao consultar a série {Serie} ({Nome}) do BCB; usando fallback.", serie, nomeSerie);
                return null;
            }
        }
    }
}
