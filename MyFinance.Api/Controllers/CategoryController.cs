using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using System.Security.Claims;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Controlador responsável pela gestão de categorias de transações do usuário.
/// Fornece endpoints para criar, obter, atualizar e deletar categorias.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    /// <summary>
    /// Inicializa uma nova instância do controlador de categorias com o serviço de categorias injetado.
    /// </summary>
    /// <param name="categoryService">Serviço responsável pela lógica de negócio das categorias</param>
    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
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
            throw new InvalidOperationException("Usuãrio não autenticado.");
        }
        return new Guid(userIdString);
    }

    /// <summary>
    /// Cria uma nova categoria de transações para o usuário autenticado.
    /// </summary>
    /// <param name="requestDto">Dados da categoria a ser criada</param>
    /// <returns>Retorna 201 (Created) com os dados da categoria criada, ou 400 (BadRequest) se houver erro</returns>
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _categoryService.CreateCategoryAsync(requestDto, userId);

        if (!response.Success)
        {
            return BadRequest(new { message = response.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetCategoryById), new { id = response.Data!.Id }, response.Data);
    }

    /// <summary>
    /// Retorna todas as categorias do usuário autenticado.
    /// </summary>
    /// <returns>Lista de todas as categorias do usuário com status 200 (OK)</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var userId = GetUserIdFromToken();
        var response = await _categoryService.GetAllCategoriesAsync(userId);
        return Ok(response.Data);
    }

    /// <summary>
    /// Retorna uma categoria específica do usuário autenticado pelo seu identificador.
    /// </summary>
    /// <param name="id">Identificador único (GUID) da categoria</param>
    /// <returns>Retorna 200 (OK) com os dados da categoria, ou 404 (NotFound) se a categoria não existir</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var userId = GetUserIdFromToken();
        var response = await _categoryService.GetAllCategoriesAsync(userId);
        var category = response.Data?.FirstOrDefault(c => c.Id == id);

        if (category == null)
        {
            return NotFound(new { message = "Categoria não encontrada." });
        }
        return Ok(category);
    }

    /// <summary>
    /// Atualiza os dados de uma categoria existente do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador único (GUID) da categoria a ser atualizada</param>
    /// <param name="requestDto">Novos dados da categoria</param>
    /// <returns>Retorna 200 (OK) com os dados atualizados, 404 (NotFound) se não encontrar, ou 400 (BadRequest) se houver erro</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        var response = await _categoryService.UpdateCategoryAsync(id, requestDto, userId);

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
    /// Deleta uma categoria existente do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador único (GUID) da categoria a ser deletada</param>
    /// <returns>Retorna 204 (NoContent) se bem-sucedido, 404 (NotFound) se não encontrar, ou 400 (BadRequest) se houver erro</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var userId = GetUserIdFromToken();
        var response = await _categoryService.DeleteCategoryAsync(id, userId);

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