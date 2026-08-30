using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Api.Controllers;

/// <summary>
/// "Meta reversa" da calculadora de investimentos: em vez de projetar um
/// resultado a partir de aporte e prazo, resolve o aporte mensal ou o prazo
/// necessários para atingir um valor-alvo, e permite simular isso contra uma
/// meta financeira já cadastrada pelo usuário.
/// </summary>
[ApiController]
[Route("api/investimentos/meta-reversa")]
[Authorize]
public class MetaReversaController : ControllerBase
{
    private readonly IMetaReversaService _metaReversaService;

    public MetaReversaController(IMetaReversaService metaReversaService)
    {
        _metaReversaService = metaReversaService;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            throw new InvalidOperationException("Usuário não autenticado.");
        return Guid.Parse(userIdString);
    }

    /// <summary>Calcula o aporte mensal necessário para atingir um valor-alvo dentro de um prazo fixo.</summary>
    [HttpPost("aporte-necessario")]
    public async Task<IActionResult> CalcularAporteNecessario([FromBody] CalcularAporteNecessarioRequestDto request)
    {
        try
        {
            var result = await _metaReversaService.CalcularAporteNecessarioAsync(request);
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

    /// <summary>Calcula o prazo (em meses) necessário para atingir um valor-alvo com um aporte mensal fixo.</summary>
    [HttpPost("prazo-necessario")]
    public async Task<IActionResult> CalcularPrazoNecessario([FromBody] CalcularPrazoNecessarioRequestDto request)
    {
        try
        {
            var result = await _metaReversaService.CalcularPrazoNecessarioAsync(request);
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

    /// <summary>
    /// Simula uma meta financeira do usuário autenticado: informe o aporte mensal
    /// para verificar se ele atinge a meta, ou omita-o para calcular o aporte
    /// mensal necessário.
    /// </summary>
    [HttpPost("metas/{goalId:guid}/simular")]
    public async Task<IActionResult> SimularMeta(Guid goalId, [FromBody] SimularMetaRequestDto request)
    {
        var userId = GetUserIdFromToken();
        try
        {
            var result = await _metaReversaService.SimularMetaAsync(goalId, userId, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(new { message = ex.Message });
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
