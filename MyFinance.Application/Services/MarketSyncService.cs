using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Services
{
    /// <summary>
    /// Orquestra a sincronização de cotações de mercado de todos os investimentos
    /// com Ticker configurado. Disparado uma vez a cada inicialização da API
    /// (ver StartupMarketSyncHostedService, na camada de Infrastructure/Api).
    ///
    /// Como o domínio não rastreia quantidade de cotas/ações, o ValorAtual (valor total
    /// da posição) é ajustado pela variação percentual do preço de mercado desde a última
    /// cotação registrada — não pelo preço absoluto, que representaria apenas uma unidade.
    /// </summary>
    public class MarketSyncService : IMarketSyncService
    {
        private readonly IInvestimentoRepository _investimentoRepository;
        private readonly ICotacaoHistoricoRepository _cotacaoRepository;
        private readonly IStockMarketIntegrationService _stockMarketService;
        private readonly ILogger<MarketSyncService> _logger;

        public MarketSyncService(
            IInvestimentoRepository investimentoRepository,
            ICotacaoHistoricoRepository cotacaoRepository,
            IStockMarketIntegrationService stockMarketService,
            ILogger<MarketSyncService> logger)
        {
            _investimentoRepository = investimentoRepository;
            _cotacaoRepository = cotacaoRepository;
            _stockMarketService = stockMarketService;
            _logger = logger;
        }

        public async Task SyncAllAsync()
        {
            var investimentos = (await _investimentoRepository.GetAllComTickerAsync()).ToList();
            _logger.LogInformation("Iniciando sincronização de cotações para {Count} investimento(s).", investimentos.Count);

            foreach (var investimento in investimentos)
            {
                try
                {
                    await SincronizarUmAsync(investimento);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Falha ao sincronizar cotação do investimento {InvestimentoId} (ticker {Ticker}). Prosseguindo com os demais.",
                        investimento.Id, investimento.Ticker);
                }
            }

            _logger.LogInformation("Sincronização de cotações concluída.");
        }

        private async Task SincronizarUmAsync(Investimento investimento)
        {
            var ticker = investimento.Ticker!;

            var jaTemHistorico = await _cotacaoRepository.ExisteAlgumRegistroAsync(investimento.Id);
            if (!jaTemHistorico)
            {
                var historico = await _stockMarketService.GetHistoryAsync(ticker, meses: 3);
                foreach (var ponto in historico)
                    await _cotacaoRepository.AddAsync(new CotacaoHistorico(investimento.Id, ponto.Data, ponto.Valor));

                await _cotacaoRepository.SaveChangesAsync();
            }

            var cotacaoAtual = await _stockMarketService.GetQuoteAsync(ticker);
            if (!cotacaoAtual.HasValue)
                return;

            var ultimaCotacao = await _cotacaoRepository.GetUltimaCotacaoAsync(investimento.Id);
            if (ultimaCotacao != null && ultimaCotacao.Valor > 0)
            {
                var variacao = (cotacaoAtual.Value - ultimaCotacao.Valor) / ultimaCotacao.Valor;
                investimento.AtualizarValorAtual(investimento.ValorAtual * (1 + variacao));
                _investimentoRepository.Update(investimento);
            }

            var hoje = DateTime.UtcNow.Date;
            if (!await _cotacaoRepository.ExisteParaDataAsync(investimento.Id, hoje))
                await _cotacaoRepository.AddAsync(new CotacaoHistorico(investimento.Id, hoje, cotacaoAtual.Value));

            await _cotacaoRepository.SaveChangesAsync();
            await _investimentoRepository.SaveChangesAsync();
        }
    }
}
