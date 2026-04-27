using Microsoft.AspNetCore.Mvc;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace MyFinance.Api.Controllers;

/// <summary>
/// Controlador responsável pela autenticação e registro de usuários.
/// Fornece endpoints para login e registro de novos usuários.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Inicializa uma nova instância do controlador de autenticação com o serviço de autenticação injetado.
    /// </summary>
    /// <param name="authService">Serviço responsável pela lógica de autenticação e registro de usuários</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Autentica um usuário com base nas credenciais fornecidas e retorna um token JWT.
    /// </summary>
    /// <param name="loginRequest">Dados de login contendo email e senha do usuário</param>
    /// <returns>Retorna 200 (OK) com token JWT se bem-sucedido, 401 (Unauthorized) se credenciais inválidas, ou 400 (BadRequest) se dados inválidos</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, data, errorMessage) = await _authService.AuthenticateAsync(loginRequest);

        if (!success)
        {
            return Unauthorized(new { message = errorMessage });
        }

        return Ok(data);
    }

    /// <summary>
    /// Registra um novo usuário no sistema.
    /// </summary>
    /// <param name="registerRequest">Dados do registro contendo email, senha e informações pessoais do usuário</param>
    /// <returns>Retorna 200 (OK) com mensagem de sucesso se bem-sucedido, 400 (BadRequest) se houver erro no registro ou dados inválidos</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, errorMessage) = await _authService.RegisterAsync(registerRequest);

        if (!success)
        {
            return BadRequest(new { message = errorMessage });
        }

        return Ok(new { message = "Usuário registrado com sucesso!" });
    }
}