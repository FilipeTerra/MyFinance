using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Fase de retirada (desacumulação) da calculadora de investimentos: simula
/// saques mensais fixos sobre um saldo que continua rendendo, com IR retido
/// proporcionalmente ao ganho de cada saque.
/// </summary>
[ApiController]
[Route("api/investimentos/retirada")]
[Authorize]
public class RetiradaController : ControllerBase
{
    private readonly IRetiradaService _retiradaService;

    public RetiradaController(IRetiradaService retiradaService)
    {
        _retiradaService = retiradaService;
    }

    /// <summary>Calcula o maior saque mensal (bruto) sustentável até o fim do prazo de retirada desejado.</summary>
    [HttpPost("saque-sustentavel")]
    public async Task<IActionResult> CalcularSaqueSustentavel([FromBody] CalcularSaqueSustentavelRequestDto request)
    {
        try
        {
            var result = await _retiradaService.CalcularSaqueSustentavelAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Calcula quantos meses o saldo dura com um saque mensal (bruto) fixo.</summary>
    [HttpPost("duracao")]
    public async Task<IActionResult> CalcularDuracao([FromBody] CalcularDuracaoRetiradaRequestDto request)
    {
        try
        {
            var result = await _retiradaService.CalcularDuracaoAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
