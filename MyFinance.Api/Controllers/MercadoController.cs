using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Dados de mercado (B3 e taxas de referência da economia brasileira).
///
/// Consumido pelas ferramentas do Agente de IA, que acessam estas rotas com o JWT
/// do usuário — as integrações externas ficam concentradas aqui, na Infrastructure,
/// e não são duplicadas no microsserviço Python.
/// </summary>
[ApiController]
[Route("api/mercado")]
[Authorize]
public class MercadoController : ControllerBase
{
    private readonly IStockMarketIntegrationService _stockMarketService;
    private readonly ITaxasReferenciaIntegrationService _taxasReferenciaService;

    /// <summary>
    /// Construtor do controller, injetando as integrações de mercado.
    /// </summary>
    /// <param name="stockMarketService"></param>
    /// <param name="taxasReferenciaService"></param>
    public MercadoController(
        IStockMarketIntegrationService stockMarketService,
        ITaxasReferenciaIntegrationService taxasReferenciaService)
    {
        _stockMarketService = stockMarketService;
        _taxasReferenciaService = taxasReferenciaService;
    }

    /// <summary>
    /// Indicadores fundamentalistas e preço atual de um ativo da B3.
    /// Indicadores que o provedor não disponibiliza vêm como null.
    /// </summary>
    /// <param name="ticker">Código do ativo na B3 (ex: PETR4).</param>
    [HttpGet("indicadores/{ticker}")]
    public async Task<IActionResult> GetIndicadores(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            return BadRequest(new { message = "Informe o ticker do ativo." });

        var indicadores = await _stockMarketService.GetIndicadoresAsync(ticker);
        if (indicadores == null)
            return NotFound(new { message = $"Não foram encontrados dados de mercado para o ticker {ticker.ToUpperInvariant()}." });

        return Ok(indicadores);
    }

    /// <summary>
    /// Taxas de referência da economia brasileira: Selic, IPCA, CDI e juros reais.
    /// </summary>
    [HttpGet("taxas-referencia")]
    public async Task<IActionResult> GetTaxasReferencia()
    {
        var taxas = await _taxasReferenciaService.GetTaxasReferenciaAsync();
        if (taxas == null)
            return StatusCode(503, new { message = "As taxas de referência estão indisponíveis no momento." });

        return Ok(taxas);
    }
}
