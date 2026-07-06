using Moq;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepository.Object, _passwordHasher.Object, _tokenService.Object);
    }

    private static User BuildUser(string email = "user@test.com") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Fulano",
        Email = email,
        PasswordHash = "hashed",
        CreatedAt = DateTime.UtcNow
    };

    // ---------- AuthenticateAsync ----------

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccessAndToken()
    {
        var user = BuildUser();
        var request = new LoginRequestDto { Email = user.Email, Password = "senha123" };

        _userRepository.Setup(r => r.GetUserByEmailAsync(user.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("senha123", user.PasswordHash)).Returns(true);
        _tokenService.Setup(t => t.GenerateToken(user)).Returns("jwt-token");

        var (success, data, error) = await _sut.AuthenticateAsync(request);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(data);
        Assert.Equal("jwt-token", data!.Token);
        Assert.Equal(user.Name, data.UserName);
        Assert.Equal(user.Email, data.UserEmail);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserNotFound_ReturnsFailureWithoutCheckingPassword()
    {
        var request = new LoginRequestDto { Email = "missing@test.com", Password = "x" };
        _userRepository.Setup(r => r.GetUserByEmailAsync(request.Email)).ReturnsAsync((User?)null);

        var (success, data, error) = await _sut.AuthenticateAsync(request);

        Assert.False(success);
        Assert.Null(data);
        Assert.Equal("Email ou senha inválidos.", error);
        _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _tokenService.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_ReturnsFailureAndDoesNotGenerateToken()
    {
        var user = BuildUser();
        var request = new LoginRequestDto { Email = user.Email, Password = "errada" };

        _userRepository.Setup(r => r.GetUserByEmailAsync(user.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("errada", user.PasswordHash)).Returns(false);

        var (success, data, error) = await _sut.AuthenticateAsync(request);

        Assert.False(success);
        Assert.Null(data);
        Assert.Equal("Email ou senha inválidos.", error);
        _tokenService.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    // ---------- RegisterAsync ----------

    [Fact]
    public async Task RegisterAsync_WithNewEmail_HashesPasswordAndPersistsUser()
    {
        var request = new RegisterRequestDto
        {
            Name = "Novo",
            Email = "novo@test.com",
            Password = "senhaForte",
            ConfirmPassword = "senhaForte"
        };

        _userRepository.Setup(r => r.CheckEmailExistsAsync(request.Email)).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("senhaForte")).Returns("hash-final");
        User? savedUser = null;
        _userRepository.Setup(r => r.AddUserAsync(It.IsAny<User>()))
            .Callback<User>(u => savedUser = u)
            .Returns(Task.CompletedTask);

        var (success, error) = await _sut.RegisterAsync(request);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(savedUser);
        Assert.Equal("Novo", savedUser!.Name);
        Assert.Equal("novo@test.com", savedUser.Email);
        Assert.Equal("hash-final", savedUser.PasswordHash);
        Assert.NotEqual(Guid.Empty, savedUser.Id);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsFailureAndDoesNotPersist()
    {
        var request = new RegisterRequestDto { Email = "existe@test.com", Password = "x", Name = "Y" };
        _userRepository.Setup(r => r.CheckEmailExistsAsync(request.Email)).ReturnsAsync(true);

        var (success, error) = await _sut.RegisterAsync(request);

        Assert.False(success);
        Assert.Equal("Este email já está cadastrado.", error);
        _userRepository.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
        _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenRepositoryThrows_ReturnsGenericFailure()
    {
        var request = new RegisterRequestDto { Email = "erro@test.com", Password = "x", Name = "Y" };
        _userRepository.Setup(r => r.CheckEmailExistsAsync(request.Email)).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _userRepository.Setup(r => r.AddUserAsync(It.IsAny<User>())).ThrowsAsync(new Exception("db down"));

        var (success, error) = await _sut.RegisterAsync(request);

        Assert.False(success);
        Assert.Equal("Ocorreu um erro inesperado ao tentar registrar. Tente novamente mais tarde.", error);
    }
}
