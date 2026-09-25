using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos.Analytics;
using MyFinance.Application.Interfaces.Services;
using System.Security.Claims;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Controlador responsável pela análise de gastos do usuário, usada na aba "Gastos" do dashboard.
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICommittedService _committedService;

    /// <summary>
    /// Inicializa uma nova instância do controlador de análise de gastos com os serviços injetados.
    /// </summary>
    /// <param name="analyticsService">Serviço responsável pela lógica de análise de gastos</param>
    /// <param name="committedService">Serviço responsável pelo comprometido com compras parceladas</param>
    public AnalyticsController(IAnalyticsService analyticsService, ICommittedService committedService)
    {
        _analyticsService = analyticsService;
        _committedService = committedService;
    }

    /// <summary>
    /// Extrai o identificador do usuário autenticado a partir do token JWT.
    /// </summary>
    /// <returns>GUID do usuário autenticado</returns>
    /// <exception cref="InvalidOperationException">Lançado quando o usuário não está autenticado</exception>
    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            throw new InvalidOperationException("Usuário não autenticado.");
        }
        return new Guid(userIdString);
    }

    /// <summary>
    /// Retorna a visão geral de despesas/receitas do período informado, com ranking por categoria,
    /// maiores lançamentos e comparação com o período anterior de mesma duração.
    /// </summary>
    /// <param name="filters">Filtros de período e conta (Months é ignorado neste endpoint)</param>
    /// <returns>Retorna 200 (OK) com a visão geral, ou 400 (BadRequest) se os filtros forem inválidos</returns>
    [HttpGet("expenses/overview")]
    public async Task<IActionResult> GetExpenseOverview([FromQuery] ExpenseAnalyticsFilterDto filters)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _analyticsService.GetExpenseOverviewAsync(userId, filters);

        if (!response.Success)
        {
            return BadRequest(new { message = response.ErrorMessage });
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Retorna a evolução mensal de despesas/receitas e a composição por categoria, mês a mês,
    /// sem lacunas no período.
    /// </summary>
    /// <param name="filters">Filtros de período, conta e quantidade de meses</param>
    /// <returns>Retorna 200 (OK) com a linha do tempo, ou 400 (BadRequest) se os filtros forem inválidos</returns>
    [HttpGet("expenses/timeline")]
    public async Task<IActionResult> GetExpenseTimeline([FromQuery] ExpenseAnalyticsFilterDto filters)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _analyticsService.GetExpenseTimelineAsync(userId, filters);

        if (!response.Success)
        {
            return BadRequest(new { message = response.ErrorMessage });
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Retorna o comprometido: o que o usuário já deve dos próximos meses por compras
    /// parceladas no cartão, com a lista de compras em aberto e o cronograma mês a mês.
    /// Não recebe período — o compromisso é sempre a partir de hoje.
    /// </summary>
    /// <param name="accountId">Conta a analisar. Omitido, considera todas as contas do usuário.</param>
    /// <returns>Retorna 200 (OK) com o comprometido, ou 400 (BadRequest) em caso de falha</returns>
    [HttpGet("expenses/committed")]
    public async Task<IActionResult> GetCommitted([FromQuery] Guid? accountId)
    {
        var userId = GetUserIdFromToken();
        var response = await _committedService.GetCommittedAsync(userId, accountId);

        if (!response.Success)
        {
            return BadRequest(new { message = response.ErrorMessage });
        }

        return Ok(response.Data);
    }
}
