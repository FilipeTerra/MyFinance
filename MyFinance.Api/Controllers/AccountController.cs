using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using System.Security.Claims;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Controlador responsável pela gestão de contas do usuário.
/// Fornece endpoints para criar, obter, atualizar e deletar contas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    /// <summary>
    /// Inicializa uma nova instância do controlador de contas com o serviço de contas injetado.
    /// </summary>
    /// <param name="accountService">Serviço responsável pela lógica de negócio das contas</param>
    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
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
    /// Cria uma nova conta de usuário no sistema.
    /// </summary>
    /// <param name="requestDto">Dados da conta a ser criada</param>
    /// <returns>Retorna 201 (Created) com os dados da conta criada, ou 400 (BadRequest) se houver erro</returns>
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

        return CreatedAtAction(nameof(GetAccountById), new { id = response.Data!.Id }, response.Data);
    }

    /// <summary>
    /// Retorna todas as contas do usuário autenticado.
    /// </summary>
    /// <returns>Lista de todas as contas do usuário com status 200 (OK)</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllAccounts()
    {
        var userId = GetUserIdFromToken();
        var response = await _accountService.GetAllAccountsAsync(userId);

        return Ok(response.Data);
    }

    /// <summary>
    /// Retorna uma conta específica do usuário autenticado pelo seu identificador.
    /// </summary>
    /// <param name="id">Identificador único (GUID) da conta</param>
    /// <returns>Retorna 200 (OK) com os dados da conta, ou 404 (NotFound) se a conta não existir</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var userId = GetUserIdFromToken();
        var response = await _accountService.GetAllAccountsAsync(userId);
        var account = response.Data?.FirstOrDefault(a => a.Id == id);

        if (account == null)
        {
            return NotFound(new { message = "Conta não encontrada." });
        }
        return Ok(account);
    }

    /// <summary>
    /// Atualiza os dados de uma conta existente do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador único (GUID) da conta a ser atualizada</param>
    /// <param name="requestDto">Novos dados da conta</param>
    /// <returns>Retorna 200 (OK) com os dados atualizados, 404 (NotFound) se não encontrar, ou 400 (BadRequest) se houver erro</returns>
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
            if (response.ErrorMessage!.Contains("não encontrada"))
            {
                return NotFound(new { message = response.ErrorMessage });
            }
            return BadRequest(new { message = response.ErrorMessage });
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Deleta uma conta existente do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador único (GUID) da conta a ser deletada</param>
    /// <returns>Retorna 204 (NoContent) se bem-sucedido, 404 (NotFound) se não encontrar, ou 400 (BadRequest) se houver erro</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var userId = GetUserIdFromToken();
        var response = await _accountService.DeleteAccountAsync(id, userId);

        if (!response.Success)
        {
            if (response.ErrorMessage!.Contains("não encontrada"))
            {
                return NotFound(new { message = response.ErrorMessage });
            }
            return BadRequest(new { message = response.ErrorMessage });
        }

        return NoContent();
    }
}