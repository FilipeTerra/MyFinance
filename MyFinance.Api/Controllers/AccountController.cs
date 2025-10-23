using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using System;
using System.Security.Claims; // Para ler o Id do usuário do token

namespace MyFinance.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // Rota base: /api/accounts
[Authorize] // Protege TODOS os endpoints neste controller
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    // --- Função Helper para pegar o UserId do Token (JwtRegisteredClaimNames.Sub) ---
    private Guid GetUserIdFromToken()
    {
        // Busca a claim "sub" (Subject) do token, que configuramos no AuthService
        //
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            // Isso não deve acontecer se [Authorize] estiver funcionando
            throw new InvalidOperationException("Usuário não autenticado.");
        }
        return new Guid(userIdString);
    }

    // POST /api/accounts
    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] AccountRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _accountService.CreateAccountAsync(requestDto, userId);

        if (!response.Success)
        {
            return BadRequest(new { message = response.ErrorMessage });
        }

        // Retorna 201 Created com o objeto criado e o local (URL)
        return CreatedAtAction(nameof(GetAccountById), new { id = response.Data!.Id }, response.Data);
    }

    // GET /api/accounts
    [HttpGet]
    public async Task<IActionResult> GetAllAccounts()
    {
        var userId = GetUserIdFromToken();
        var response = await _accountService.GetAllAccountsAsync(userId);

        // response.Data nunca será nulo aqui, na pior das hipóteses é uma lista vazia
        return Ok(response.Data);
    }

    // GET /api/accounts/{id} 
    // (Necessário para o 'CreatedAtAction' do Create)
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var userId = GetUserIdFromToken();
        // Usamos o Service para garantir que o usuário só possa pegar a *sua* conta
        var response = await _accountService.GetAllAccountsAsync(userId);
        var account = response.Data?.FirstOrDefault(a => a.Id == id);

        if (account == null)
        {
            return NotFound(new { message = "Conta não encontrada." });
        }
        return Ok(account);
    }

    // PUT /api/accounts/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _accountService.UpdateAccountAsync(id, requestDto, userId);

        if (!response.Success)
        {
            // Se a conta não for encontrada ou não pertencer ao usuário
            if (response.ErrorMessage!.Contains("não encontrada"))
            {
                return NotFound(new { message = response.ErrorMessage });
            }
            return BadRequest(new { message = response.ErrorMessage });
        }

        return Ok(response.Data); // Retorna a conta atualizada
    }

    // DELETE /api/accounts/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var userId = GetUserIdFromToken();
        var response = await _accountService.DeleteAccountAsync(id, userId);

        if (!response.Success)
        {
            // Se a conta não for encontrada
            if (response.ErrorMessage!.Contains("não encontrada"))
            {
                return NotFound(new { message = response.ErrorMessage });
            }
            // Se for outra regra (ex: conta com transações)
            return BadRequest(new { message = response.ErrorMessage });
        }

        return NoContent(); // Sucesso (204), sem conteúdo para retornar
    }
}