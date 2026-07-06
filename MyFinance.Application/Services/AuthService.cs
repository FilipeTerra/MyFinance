using System;
using System.Threading.Tasks;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<(bool Success, LoginResponseDto? Data, string? ErrorMessage)> AuthenticateAsync(LoginRequestDto loginRequest)
    {
        // Encontrar o usuário pelo email
        var user = await _userRepository.GetUserByEmailAsync(loginRequest.Email);

        // Verificar se o usuário existe e se a senha está correta
        if (user == null || !_passwordHasher.Verify(loginRequest.Password, user.PasswordHash))
        {
            return (false, null, "Email ou senha inválidos."); // Critário de Aceitação
        }

        // Gerar o Token JWT
        var token = _tokenService.GenerateToken(user);

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
        // Verificar se o email já existe (Critário de Aceitação)
        var emailExists = await _userRepository.CheckEmailExistsAsync(registerRequest.Email);
        if (emailExists)
        {
            return (false, "Este email já está cadastrado.");
        }

        // Criar o hash da senha (Critário de Aceitação)
        var passwordHash = _passwordHasher.Hash(registerRequest.Password);

        // Criar a nova entidade User
        var newUser = new User
        {
            Id = Guid.NewGuid(), // Gerar um novo ID ánico
            Name = registerRequest.Name,
            Email = registerRequest.Email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow // Usar UTC para datas no servidor
        };

        // Adicionar o usuário ao banco de dados
        try
        {
            await _userRepository.AddUserAsync(newUser);
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao registrar usuário: {ex.Message}"); // Apenas para debug
            return (false, "Ocorreu um erro inesperado ao tentar registrar. Tente novamente mais tarde.");
        }
    }
}
