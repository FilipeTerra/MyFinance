using Microsoft.AspNetCore.Mvc; // Para [ApiController], [Route], ControllerBase, etc.
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace MyFinance.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // Rota base será /api/auth
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")] // Rota completa: POST /api/auth/login
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
    {
        // Validação básica do DTO (verifica os [Required], [EmailAddress])
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, data, errorMessage) = await _authService.AuthenticateAsync(loginRequest);

        if (!success)
        {
            // Retorna 401 Unauthorized com a mensagem de erro
            return Unauthorized(new { message = errorMessage });
        }

        return Ok(data);
    }

    [HttpPost("register")] // Rota completa: POST /api/auth/register
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequest)
    {
        // Validação básica do DTO (verifica [Required], [Compare], etc.)
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, errorMessage) = await _authService.RegisterAsync(registerRequest);

        if (!success)
        {
            // Retorna 400 Bad Request com a mensagem de erro (ex: email já existe)
            return BadRequest(new { message = errorMessage });
        }

        // Retorna 201 Created 
        return Ok(new { message = "Usuário registrado com sucesso!" });
    }
}