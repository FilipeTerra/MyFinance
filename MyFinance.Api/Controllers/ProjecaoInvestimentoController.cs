using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Calculadora de projeção de investimento a longo prazo (juros compostos com
/// aportes mensais). Stateless — não persiste nem exige um investimento existente.
/// </summary>
[ApiController]
[Route("api/investimentos/projecao")]
[Authorize]
public class ProjecaoInvestimentoController : ControllerBase
{
    private readonly IProjecaoInvestimentoService _projecaoInvestimentoService;

    /// <summary>
    /// Construtor do controller, injetando o serviço de projeção de investimentos.
    /// </summary>
    /// <param name="projecaoInvestimentoService"></param>
    public ProjecaoInvestimentoController(IProjecaoInvestimentoService projecaoInvestimentoService)
    {
        _projecaoInvestimentoService = projecaoInvestimentoService;
    }

    /// <summary>
    /// Calcula a projeção de um investimento com aportes mensais constantes.
    /// Quando <c>usarTaxaSelic</c> é verdadeiro, a taxa de juros é obtida
    /// automaticamente via Selic real (Banco Central).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CalcularProjecao([FromBody] CalcularProjecaoRequestDto request)
    {
        try
        {
            var result = await _projecaoInvestimentoService.CalcularProjecaoAsync(request);
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
