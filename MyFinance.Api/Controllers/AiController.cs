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

    /// <summary>
    /// Construtor do controller, injetando o serviço de integração com os agentes de IA.
    /// </summary>
    /// <param name="aiIntegrationService"></param>
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

    /// <summary>
    /// Dispara o Monitor de Inflação do Estilo de Vida: analisa os últimos 6 meses de
    /// transações e verifica se gastos supérfluos (lazer, restaurantes, assinaturas)
    /// estão crescendo mais rápido que os investimentos, retornando um alerta educativo
    /// embasado em literatura de finanças pessoais quando aplicável.
    /// </summary>
    [HttpGet("insights/lifestyle-inflation")]
    public async Task<IActionResult> GetLifestyleInflationInsight()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        var jwtToken = authHeader.StartsWith("Bearer ") ? authHeader["Bearer ".Length..] : authHeader;

        if (string.IsNullOrEmpty(jwtToken))
            return Unauthorized(new { message = "Usuário não autenticado." });

        try
        {
            var insight = await _aiIntegrationService.GetLifestyleInflationInsightAsync(jwtToken);
            return Ok(insight);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Falha de comunicação com o Agente IA: {ex.Message}" });
        }
    }
}
