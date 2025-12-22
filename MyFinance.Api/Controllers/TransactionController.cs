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
[Authorize] // Todas as rotas aqui exigem autentica��o
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    // --- Fun��o Helper para pegar o UserId do Token ---
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
            // Erros como "Conta n�o encontrada" podem ser BadRequest ou NotFound,
            // mas BadRequest � mais simples por enquanto.
            return BadRequest(new { message = response.ErrorMessage });
        }

        // Retorna 201 Created com a transa��o criada
        return CreatedAtAction(nameof(GetTransactionById), new { id = response.Data!.Id }, response.Data);
    }

    // GET /api/transactions/account/{accountId} 
    // Rota espec�fica para pegar transa��es POR CONTA (US 8: Filtro)
    [HttpGet("account/{accountId:guid}")]
    public async Task<IActionResult> GetTransactionsByAccount(Guid accountId)
    {
        var userId = GetUserIdFromToken();
        var response = await _transactionService.GetTransactionsByAccountIdAsync(accountId, userId);

        // O servi�o retorna lista vazia se a conta n�o for do usu�rio, ent�o OK � seguro
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

    [HttpGet("search")]
    public async Task<IActionResult> SearchTransactions([FromQuery] TransactionSearchRequestDto filters)
    {
        // O [FromQuery] mapeia os par�metros da URL (ex: ?accountId=...) para o objeto DTO.
        if (!ModelState.IsValid)
        {
            // Retorna 400 Bad Request se o AccountId n�o for fornecido,
            // conforme a valida��o [Required] no DTO.
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _transactionService.SearchTransactionsAsync(userId, filters);

        // O servi�o sempre retorna uma lista (Data nunca � nulo),
        // mesmo que esteja vazia.
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
            if (response.ErrorMessage!.Contains("n�o encontrada"))
            {
                // Pode ser a transa��o ou a nova conta
                return NotFound(new { message = response.ErrorMessage });
            }
            return BadRequest(new { message = response.ErrorMessage });
        }

        return Ok(response.Data); // Retorna a transa��o atualizada
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