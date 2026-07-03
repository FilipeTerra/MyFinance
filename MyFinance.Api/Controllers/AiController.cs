using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Controller responsável por acionar os agentes proativos de IA financeira.
/// </summary>
[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiIntegrationService _aiIntegrationService;

    public AiController(IAiIntegrationService aiIntegrationService)
    {
        _aiIntegrationService = aiIntegrationService;
    }

    /// <summary>
    /// Dispara o Agente Proativo de Reserva de Emergência: verifica se o valor
    /// guardado pelo usuário (metas de reserva + investimentos de Renda Fixa)
    /// atinge o ideal de 6x a renda mensal, e retorna um diagnóstico em linguagem natural.
    /// </summary>
    [HttpGet("insights/emergency-reserve")]
    public async Task<IActionResult> GetEmergencyReserveInsight()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        var jwtToken = authHeader.StartsWith("Bearer ") ? authHeader["Bearer ".Length..] : authHeader;

        if (string.IsNullOrEmpty(jwtToken))
            return Unauthorized(new { message = "Usuário não autenticado." });

        try
        {
            var insight = await _aiIntegrationService.GetEmergencyReserveInsightAsync(jwtToken);
            return Ok(insight);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Falha de comunicação com o Agente IA: {ex.Message}" });
        }
    }
}
