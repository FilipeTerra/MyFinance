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
[Authorize] // Protege TODOS os endpoints neste controller
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            throw new InvalidOperationException("Usuário não autenticado.");
        }
        return new Guid(userIdString);
    }

    // POST /api/categories
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

    // GET /api/categories
    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var userId = GetUserIdFromToken();
        var response = await _categoryService.GetAllCategoriesAsync(userId);
        return Ok(response.Data);
    }

    // GET /api/categories/{id}
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

    // PUT /api/categories/{id}
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

    // DELETE /api/categories/{id}
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
            // Ex: "Não é possível excluir..."
            return BadRequest(new { message = response.ErrorMessage });
        }

        return NoContent(); // Sucesso (204)
    }
}