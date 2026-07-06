using Moq;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepository.Object);
    }

    private static User BuildUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Fulano",
        Email = "fulano@test.com",
        PasswordHash = "hash",
        CreatedAt = DateTime.UtcNow,
        MonthlyIncome = 5000m
    };

    // ---------- GetProfileAsync ----------

    [Fact]
    public async Task GetProfileAsync_WhenUserExists_ReturnsProfile()
    {
        var user = BuildUser();
        _userRepository.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.GetProfileAsync(user.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data!.Id);
        Assert.Equal(user.Name, result.Data.Name);
        Assert.Equal(user.Email, result.Data.Email);
        Assert.Equal(5000m, result.Data.MonthlyIncome);
    }

    [Fact]
    public async Task GetProfileAsync_WhenUserNotFound_ReturnsFailure()
    {
        _userRepository.Setup(r => r.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var result = await _sut.GetProfileAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Usuário não encontrado.", result.ErrorMessage);
    }

    // ---------- UpdateProfileAsync ----------

    [Fact]
    public async Task UpdateProfileAsync_WhenUserExists_UpdatesMonthlyIncomeAndPersists()
    {
        var user = BuildUser();
        _userRepository.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        var request = new UpdateUserProfileRequestDto { MonthlyIncome = 8000m };

        var result = await _sut.UpdateProfileAsync(user.Id, request);

        Assert.True(result.Success);
        Assert.Equal(8000m, result.Data!.MonthlyIncome);
        Assert.Equal(8000m, user.MonthlyIncome);
        _userRepository.Verify(r => r.UpdateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserNotFound_ReturnsFailureAndDoesNotPersist()
    {
        _userRepository.Setup(r => r.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var result = await _sut.UpdateProfileAsync(Guid.NewGuid(), new UpdateUserProfileRequestDto { MonthlyIncome = 1m });

        Assert.False(result.Success);
        Assert.Equal("Usuário não encontrado.", result.ErrorMessage);
        _userRepository.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }
}
