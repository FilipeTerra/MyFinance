using System;
using System.IdentityModel.Tokens.Jwt; // Para JwtSecurityTokenHandler
using System.Security.Claims; // Para Claims
using System.Text; // Para Encoding
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; // Para ler appsettings
using Microsoft.IdentityModel.Tokens; // Para SymmetricSecurityKey
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;
using BCrypt.Net;

namespace MyFinance.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration; // Para ler as JwtSettings

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<(bool Success, LoginResponseDto? Data, string? ErrorMessage)> AuthenticateAsync(LoginRequestDto loginRequest)
    {
        // Encontrar o usu�rio pelo email
        var user = await _userRepository.GetUserByEmailAsync(loginRequest.Email);

        // Verificar se o usu�rio existe e se a senha est� correta
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
        {
            return (false, null, "Email ou senha inv�lidos."); // Crit�rio de Aceita��o
        }

        // Gerar o Token JWT
        var token = GenerateJwtToken(user);

        var response = new LoginResponseDto
        {
            Token = token,
            UserName = user.Name,
            UserEmail = user.Email
        };

        return (true, response, null); // Sucesso
    }

    public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(RegisterRequestDto registerRequest)
    {
        // Verificar se o email j� existe (Crit�rio de Aceita��o)
        var emailExists = await _userRepository.CheckEmailExistsAsync(registerRequest.Email);
        if (emailExists)
        {
            return (false, "Este email j� est� cadastrado.");
        }

        // Criar o hash da senha (Crit�rio de Aceita��o)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);

        // Criar a nova entidade User
        var newUser = new User
        {
            Id = Guid.NewGuid(), // Gerar um novo ID �nico
            Name = registerRequest.Name,
            Email = registerRequest.Email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow // Usar UTC para datas no servidor
        };

        // Adicionar o usu�rio ao banco de dados
        try
        {
            await _userRepository.AddUserAsync(newUser);
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao registrar usu�rio: {ex.Message}"); // Apenas para debug
            return (false, "Ocorreu um erro inesperado ao tentar registrar. Tente novamente mais tarde.");
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        // Obter a chave secreta do appsettings
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]
            ?? throw new InvalidOperationException("Chave secreta JWT n�o configurada."));

        // Obter tempo de expira��o
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        // Criar as "claims" (informa��es dentro do token)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Id do usu�rio (Subject)
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            // Adicione outras claims se precisar (ex: roles)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Identificador �nico do token
        };

        // Configurar o descritor do token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        // Criar e serializar o token
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}