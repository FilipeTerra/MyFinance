using Moq;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly AccountService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AccountServiceTests()
    {
        _sut = new AccountService(_accountRepository.Object);
    }

    // ---------- CreateAccountAsync ----------

    [Fact]
    public async Task CreateAccountAsync_PersistsAccountWithInitialBalanceAndUserFromToken()
    {
        var dto = new AccountRequestDto { Name = "Nubank", Type = AccountType.ContaCorrente, InitialBalance = 500m };
        Account? saved = null;
        _accountRepository.Setup(r => r.AddAsync(It.IsAny<Account>()))
            .Callback<Account>(a => saved = a)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAccountAsync(dto, _userId);

        Assert.True(result.Success);
        Assert.Equal("Nubank", result.Data!.Name);
        Assert.Equal(500m, result.Data.InitialBalance);
        Assert.Equal(500m, result.Data.CurrentBalance);
        Assert.Equal(_userId, result.Data.UserId);
        Assert.NotNull(saved);
        Assert.Equal(_userId, saved!.UserId);
        Assert.Equal(500m, saved.Balance);
        _accountRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ---------- GetAllAccountsAsync ----------

    [Fact]
    public async Task GetAllAccountsAsync_ReturnsMappedAccounts()
    {
        var accounts = new List<Account>
        {
            new("Conta A", AccountType.ContaCorrente, 100m, _userId),
            new("Conta B", AccountType.Poupanca, 200m, _userId)
        };
        _accountRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(accounts);

        var result = await _sut.GetAllAccountsAsync(_userId);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count());
    }

    // ---------- UpdateAccountAsync ----------

    [Fact]
    public async Task UpdateAccountAsync_WhenExists_RenamesAndPersists()
    {
        var account = new Account("Antigo", AccountType.ContaCorrente, 100m, _userId);
        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        var dto = new UpdateAccountRequestDto { Name = "Novo Nome", Type = AccountType.Poupanca };

        var result = await _sut.UpdateAccountAsync(account.Id, dto, _userId);

        Assert.True(result.Success);
        Assert.Equal("Novo Nome", result.Data!.Name);
        Assert.Equal(AccountType.Poupanca, account.Type);
        _accountRepository.Verify(r => r.Update(account), Times.Once);
        _accountRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAccountAsync_DoesNotChangeBalance()
    {
        var account = new Account("Conta", AccountType.ContaCorrente, 100m, _userId);
        account.UpdateBalance(50m); // saldo agora 150
        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        var dto = new UpdateAccountRequestDto { Name = "Renomeada", Type = AccountType.Carteira };

        var result = await _sut.UpdateAccountAsync(account.Id, dto, _userId);

        Assert.Equal(150m, result.Data!.CurrentBalance);
    }

    [Fact]
    public async Task UpdateAccountAsync_WhenNotFound_ReturnsFailure()
    {
        _accountRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Account?)null);

        var result = await _sut.UpdateAccountAsync(Guid.NewGuid(), new UpdateAccountRequestDto { Name = "X", Type = AccountType.Carteira }, _userId);

        Assert.False(result.Success);
        _accountRepository.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
    }

    // ---------- DeleteAccountAsync ----------

    [Fact]
    public async Task DeleteAccountAsync_WhenExists_Deletes()
    {
        var account = new Account("Conta", AccountType.ContaCorrente, 100m, _userId);
        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);

        var result = await _sut.DeleteAccountAsync(account.Id, _userId);

        Assert.True(result.Success);
        Assert.True(result.Data);
        _accountRepository.Verify(r => r.Delete(account), Times.Once);
        _accountRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenNotFound_ReturnsFailure()
    {
        _accountRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Account?)null);

        var result = await _sut.DeleteAccountAsync(Guid.NewGuid(), _userId);

        Assert.False(result.Success);
        _accountRepository.Verify(r => r.Delete(It.IsAny<Account>()), Times.Never);
    }
}
