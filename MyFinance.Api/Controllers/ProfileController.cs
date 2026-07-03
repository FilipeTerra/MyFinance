using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using System.Security.Claims;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Controlador responsável pelo perfil do usuário autenticado.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            throw new InvalidOperationException("Usuário não autenticado.");
        return new Guid(userIdString);
    }

    /// <summary>
    /// Retorna os dados do perfil do usuário autenticado.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserIdFromToken();
        var response = await _userService.GetProfileAsync(userId);
        if (!response.Success)
            return NotFound(new { message = response.ErrorMessage });
        return Ok(response.Data);
    }

    /// <summary>
    /// Atualiza os dados do perfil do usuário autenticado.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequestDto request)
    {
        var userId = GetUserIdFromToken();
        var response = await _userService.UpdateProfileAsync(userId, request);
        if (!response.Success)
            return NotFound(new { message = response.ErrorMessage });
        return Ok(response.Data);
    }
}
