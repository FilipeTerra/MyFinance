using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Services;
using System.Security.Claims;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Corpo da requisição para atualizar o valor de mercado de um investimento.
/// </summary>
/// <param name="ValorAtual">Novo valor atual do ativo.</param>
public record UpdateValorAtualRequestDto(decimal ValorAtual);

/// <summary>
/// Controller responsável pela gestão de investimentos do usuário autenticado.
/// Expõe endpoints para criar, listar, atualizar o valor de mercado e excluir
/// investimentos. Todos os endpoints exigem autenticação e operam apenas sobre
/// os investimentos do próprio usuário.
/// </summary>
[ApiController]
[Route("api/investimentos")]
[Authorize]
public class InvestimentoController : ControllerBase
{
    private readonly IInvestimentoService _investimentoService;

    /// <summary>
    /// Construtor do controller, injetando o serviço de investimentos.
    /// </summary>
    /// <param name="investimentoService"></param>
    public InvestimentoController(IInvestimentoService investimentoService)
    {
        _investimentoService = investimentoService;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            throw new InvalidOperationException("Usuário não autenticado.");
        return Guid.Parse(userIdString);
    }

    /// <summary>
    /// Cria um novo investimento para o usuário autenticado. Retorna o investimento
    /// criado com status 201 Created.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateInvestimento([FromBody] CreateInvestimentoRequestDto request)
    {
        var userId = GetUserIdFromToken();
        try
        {
            var result = await _investimentoService.CreateInvestimentoAsync(userId, request);
            return CreatedAtAction(nameof(GetUserInvestimentos), result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lista todos os investimentos do usuário autenticado. Retorna status 200 OK.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetUserInvestimentos()
    {
        var userId = GetUserIdFromToken();
        var result = await _investimentoService.GetUserInvestimentosAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Atualiza o valor de mercado de um investimento específico. Retorna o
    /// investimento atualizado com status 200 OK.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/valor-atual")]
    public async Task<IActionResult> UpdateValorAtual(Guid id, [FromBody] UpdateValorAtualRequestDto request)
    {
        var userId = GetUserIdFromToken();
        try
        {
            var result = await _investimentoService.UpdateValorAtualAsync(id, userId, request.ValorAtual);
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
    }

    /// <summary>
    /// Exclui um investimento específico do usuário autenticado. Retorna 204 No Content.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteInvestimento(Guid id)
    {
        var userId = GetUserIdFromToken();
        try
        {
            await _investimentoService.DeleteInvestimentoAsync(id, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
