using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos.Financiamento;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Calculadora de financiamento: simula um mesmo empréstimo pelos sistemas
/// Price (parcelas fixas) e SAC (amortização constante), e converte taxas
/// nominais (APR) em taxas efetivas anuais (EAR). Stateless — não persiste nada.
/// </summary>
[ApiController]
[Route("api/financiamento")]
[Authorize]
public class FinanciamentoController : ControllerBase
{
    private readonly IFinanciamentoService _financiamentoService;

    public FinanciamentoController(IFinanciamentoService financiamentoService)
    {
        _financiamentoService = financiamentoService;
    }

    /// <summary>Simula o financiamento pelos sistemas Price e SAC e compara o total de juros pago em cada um.</summary>
    [HttpPost("simular")]
    public async Task<IActionResult> Simular([FromBody] FinanciamentoRequestDto request)
    {
        try
        {
            var result = await _financiamentoService.SimularAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Converte uma taxa nominal anual (APR) com capitalização periódica na taxa efetiva anual (EAR) equivalente.</summary>
    [HttpPost("taxa-efetiva")]
    public async Task<IActionResult> CalcularTaxaEfetiva([FromBody] TaxaEfetivaRequestDto request)
    {
        try
        {
            var result = await _financiamentoService.CalcularTaxaEfetivaAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
