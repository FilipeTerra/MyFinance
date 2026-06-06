using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos.FinancialGoals;
using MyFinance.Application.Interfaces.Services;
using System.Security.Claims;

namespace MyFinance.Api.Controllers;
/// <summary>
/// Controller para gerenciamento de metas financeiras, permitindo criação, consulta e adição de fundos às metas do usuário autenticado.
/// </summary>
/// <param name="Amount"></param>
public record AddFundsRequestDto(decimal Amount);

/// <summary>
/// Controller responsável por gerenciar as metas financeiras dos usuários. Ele expõe endpoints para criar novas metas, listar as metas existentes do usuário e adicionar fundos a uma meta específica. Todos os endpoints exigem autenticação, garantindo que apenas o usuário proprietário das metas possa acessá-las e modificá-las.
/// </summary>
[ApiController]
[Route("api/financial-goals")]
[Authorize]
public class FinancialGoalController : ControllerBase
{
    private readonly IFinancialGoalService _financialGoalService;

/// <summary>
/// Construtor do controller, injetando o serviço de metas financeiras para manipulação das operações relacionadas às metas do usuário.
/// </summary>
/// <param name="financialGoalService"></param>
    public FinancialGoalController(IFinancialGoalService financialGoalService)
    {
        _financialGoalService = financialGoalService;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            throw new InvalidOperationException("Usuário não autenticado.");
        return Guid.Parse(userIdString);
    }

/// <summary>
/// Endpoint para criação de uma nova meta financeira. Recebe os detalhes da meta no corpo da requisição e retorna a meta criada com um status 201 Created.
/// </summary>
/// <param name="request"></param>
/// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateGoal([FromBody] CreateFinancialGoalRequestDto request)
    {
        var userId = GetUserIdFromToken();
        var result = await _financialGoalService.CreateGoalAsync(userId, request);
        return CreatedAtAction(nameof(GetUserGoals), result);
    }

/// <summary>
/// Endpoint para obtenção de todas as metas financeiras do usuário autenticado. Retorna uma lista de metas com um status 200 OK.
/// </summary>
/// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetUserGoals()
    {
        var userId = GetUserIdFromToken();
        var result = await _financialGoalService.GetUserGoalsAsync(userId);
        return Ok(result);
    }

/// <summary>
/// Endpoint para adicionar fundos a uma meta financeira específica. Recebe o ID da meta na URL e o valor a ser adicionado no corpo da requisição. Retorna um status 200 OK se a operação for bem-sucedida.
/// </summary>
/// <param name="id"></param>
/// <param name="request"></param>
/// <returns></returns>
    [HttpPost("{id:guid}/add-funds")]
    public async Task<IActionResult> AddFunds(Guid id, [FromBody] AddFundsRequestDto request)
    {
        var userId = GetUserIdFromToken();
        await _financialGoalService.AddFundsToGoalAsync(id, userId, request.Amount);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGoal(Guid id)
    {
        var userId = GetUserIdFromToken();
        try
        {
            await _financialGoalService.DeleteGoalAsync(id, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
