using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Controlador responsável pela gestão de transações financeiras do usuário.
/// Fornece endpoints para criar, obter, buscar, atualizar e deletar transações.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    /// <summary>
    /// Inicializa uma nova instância do controlador de transações com o serviço de transações injetado.
    /// </summary>
    /// <param name="transactionService">Serviço responsável pela lógica de negócio das transações</param>
    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
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
    /// Cria uma nova transação para o usuário autenticado.
    /// </summary>
    /// <param name="requestDto">Dados da transação a ser criada</param>
    /// <returns>Retorna 201 (Created) com os dados da transação criada, ou 400 (BadRequest) se houver erro</returns>
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
            return BadRequest(new { message = response.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetTransactionById), new { id = response.Data!.Id }, response.Data);
    }

/// <summary>
/// Endpoint para upload de extrato bancário/cartão de crédito (CSV ou PDF) e processamento via Agente de IA.
/// </summary>
/// <param name="file"></param>
/// <param name="accountId"></param>
/// <returns></returns>
[HttpPost("upload")]
public async Task<IActionResult> UploadExtrato(IFormFile file, [FromForm] Guid accountId)
{
    if (file == null || file.Length == 0)
        return BadRequest(new { message = "Nenhum arquivo enviado." });

    using var client = new HttpClient();
    using var content = new MultipartFormDataContent();

    using var fileStream = file.OpenReadStream();
    var fileContent = new StreamContent(fileStream);
    
    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
    content.Add(fileContent, "file", file.FileName);

    content.Add(new StringContent(accountId.ToString()), "accountId");

    try
    {
        var response = await client.PostAsync("http://127.0.0.1:8181/api/ai/process-statement", content);

        if (response.IsSuccessStatusCode)
        {
            var aiResult = await response.Content.ReadAsStringAsync();
            return Content(aiResult, "application/json"); 
        }

        return StatusCode((int)response.StatusCode, new { message = "Erro ao processar no Agente de IA." });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = $"Falha de comunicação com o Agente IA: {ex.Message}" });
    }
}

    /// <summary>
    /// Retorna todas as transações de uma conta específica do usuário autenticado.
    /// </summary>
    /// <param name="accountId">Identificador único (GUID) da conta</param>
    /// <returns>Lista de transações da conta com status 200 (OK)</returns>
    [HttpGet("account/{accountId:guid}")]
    public async Task<IActionResult> GetTransactionsByAccount(Guid accountId)
    {
        var userId = GetUserIdFromToken();
        var response = await _transactionService.GetTransactionsByAccountIdAsync(accountId, userId);

        return Ok(response.Data);
    }

    /// <summary>
    /// Retorna uma transação específica do usuário autenticado pelo seu identificador.
    /// </summary>
    /// <param name="id">Identificador único (GUID) da transação</param>
    /// <returns>Retorna 200 (OK) com os dados da transação, ou 404 (NotFound) se a transação não existir</returns>
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

    /// <summary>
    /// Busca transações do usuário autenticado com base em filtros fornecidos.
    /// </summary>
    /// <param name="filters">Filtros de busca como data, tipo, categoria ou valor</param>
    /// <returns>Lista de transações que atendem aos critérios de busca com status 200 (OK), ou 400 (BadRequest) se filtros inválidos</returns>
    [HttpGet("search")]
    public async Task<IActionResult> SearchTransactions([FromQuery] TransactionSearchRequestDto filters)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _transactionService.SearchTransactionsAsync(userId, filters);

        return Ok(response.Data);
    }

    /// <summary>
    /// Atualiza os dados de uma transação existente do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador único (GUID) da transação a ser atualizada</param>
    /// <param name="requestDto">Novos dados da transação</param>
    /// <returns>Retorna 200 (OK) com os dados atualizados, 404 (NotFound) se não encontrar, ou 400 (BadRequest) se houver erro</returns>
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
            if (response.ErrorMessage!.Contains("Transação não encontrada"))
            {
                return NotFound(new { message = response.ErrorMessage });
            }
            return BadRequest(new { message = response.ErrorMessage });
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Deleta uma transação existente do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador único (GUID) da transação a ser deletada</param>
    /// <returns>Retorna 204 (NoContent) se bem-sucedido, 404 (NotFound) se não encontrar</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetUserIdFromToken();
        var response = await _transactionService.DeleteTransactionAsync(id, userId);

        if (!response.Success)
        {
            return NotFound(new { message = response.ErrorMessage });
        }

        return NoContent();
    }
}