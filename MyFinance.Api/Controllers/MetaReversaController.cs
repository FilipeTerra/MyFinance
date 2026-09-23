using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Sugestao;
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
    private readonly ISugestaoAporteService _sugestaoAporteService;

    /// <summary>
    /// Inicializa o controller com os serviços de meta reversa e de sugestão de aporte.
    /// </summary>
    /// <param name="metaReversaService">Resolve aporte e prazo necessários para um valor-alvo.</param>
    /// <param name="sugestaoAporteService">Confronta o aporte necessário com o orçamento real do usuário.</param>
    public MetaReversaController(
        IMetaReversaService metaReversaService,
        ISugestaoAporteService sugestaoAporteService)
    {
        _metaReversaService = metaReversaService;
        _sugestaoAporteService = sugestaoAporteService;
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

    /// <summary>
    /// Sugere como o usuário precisaria se comportar financeiramente para bancar o
    /// aporte de uma meta reversa: se ele cabe na sobra mensal, quanto falta, de quais
    /// categorias o corte sairia e que prazo ou alvo tornariam a meta viável.
    /// </summary>
    /// <param name="request">Parâmetros da meta reversa já calculada.</param>
    [HttpPost("sugestao")]
    public async Task<IActionResult> ObterSugestao([FromBody] SugestaoAporteRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserIdFromToken();
        var response = await _sugestaoAporteService.ObterSugestaoAsync(userId, request);

        if (!response.Success)
            return BadRequest(new { message = response.ErrorMessage });

        return Ok(response.Data);
    }
}
