using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyFinance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Todas as rotas aqui exigem autenticação
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    // --- Função Helper para pegar o UserId do Token ---
    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            throw new InvalidOperationException("Usuário não autenticado.");
        }
        return new Guid(userIdString);
    }

    // POST /api/transactions
    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _transactionService.CreateTransactionAsync(requestDto, userId);

        if (!response.Success)
        {
            // Erros como "Conta não encontrada" podem ser BadRequest ou NotFound,
            // mas BadRequest é mais simples por enquanto.
            return BadRequest(new { message = response.ErrorMessage });
        }

        // Retorna 201 Created com a transação criada
        return CreatedAtAction(nameof(GetTransactionById), new { id = response.Data!.Id }, response.Data);
    }

    // GET /api/transactions/account/{accountId} 
    // Rota específica para pegar transações POR CONTA (US 8: Filtro)
    [HttpGet("account/{accountId:guid}")]
    public async Task<IActionResult> GetTransactionsByAccount(Guid accountId)
    {
        var userId = GetUserIdFromToken();
        var response = await _transactionService.GetTransactionsByAccountIdAsync(accountId, userId);

        // O serviço retorna lista vazia se a conta não for do usuário, então OK é seguro
        return Ok(response.Data);
    }

    // GET /api/transactions/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var userId = GetUserIdFromToken();
        var response = await _transactionService.GetTransactionByIdAsync(id, userId);

        if (!response.Success)
        {
            return NotFound(new { message = response.ErrorMessage });
        }
        return Ok(response.Data);
    }

    // PUT /api/transactions/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _transactionService.UpdateTransactionAsync(id, requestDto, userId);

        if (!response.Success)
        {
            if (response.ErrorMessage!.Contains("não encontrada"))
            {
                // Pode ser a transação ou a nova conta
                return NotFound(new { message = response.ErrorMessage });
            }
            return BadRequest(new { message = response.ErrorMessage });
        }

        return Ok(response.Data); // Retorna a transação atualizada
    }

    // DELETE /api/transactions/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetUserIdFromToken();
        var response = await _transactionService.DeleteTransactionAsync(id, userId);

        if (!response.Success)
        {
            return NotFound(new { message = response.ErrorMessage });
        }

        return NoContent(); // Sucesso (204)
    }
}