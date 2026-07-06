using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Tests.Entities;

public class AccountTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesAccountWithBalanceEqualToInitialBalance()
    {
        var account = new Account("Carteira", AccountType.Carteira, 150m, ValidUserId);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal("Carteira", account.Name);
        Assert.Equal(AccountType.Carteira, account.Type);
        Assert.Equal(150m, account.InitialBalance);
        Assert.Equal(150m, account.Balance);
        Assert.Equal(ValidUserId, account.UserId);
        Assert.True(account.CreatedAt <= DateTime.UtcNow && account.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Constructor_GeneratesDifferentIdsForEachAccount()
    {
        var first = new Account("Conta A", AccountType.ContaCorrente, 0m, ValidUserId);
        var second = new Account("Conta B", AccountType.ContaCorrente, 0m, ValidUserId);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() =>
            new Account(invalidName!, AccountType.ContaCorrente, 100m, ValidUserId));
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Account("Conta", AccountType.ContaCorrente, 100m, Guid.Empty));
    }

    [Fact]
    public void UpdateBalance_WithPositiveAmount_IncreasesBalance()
    {
        var account = new Account("Conta", AccountType.ContaCorrente, 100m, ValidUserId);

        account.UpdateBalance(50m);

        Assert.Equal(150m, account.Balance);
    }

    [Fact]
    public void UpdateBalance_WithNegativeAmount_DecreasesBalance()
    {
        var account = new Account("Conta", AccountType.ContaCorrente, 100m, ValidUserId);

        account.UpdateBalance(-30m);

        Assert.Equal(70m, account.Balance);
    }

    [Fact]
    public void UpdateBalance_DoesNotAffectInitialBalance()
    {
        var account = new Account("Conta", AccountType.ContaCorrente, 100m, ValidUserId);

        account.UpdateBalance(500m);

        Assert.Equal(100m, account.InitialBalance);
    }

    [Fact]
    public void Rename_WithValidData_UpdatesNameAndType()
    {
        var account = new Account("Nome Antigo", AccountType.ContaCorrente, 100m, ValidUserId);

        account.Rename("Nome Novo", AccountType.Poupanca);

        Assert.Equal("Nome Novo", account.Name);
        Assert.Equal(AccountType.Poupanca, account.Type);
    }

    [Fact]
    public void Rename_DoesNotAffectBalanceOrInitialBalance()
    {
        var account = new Account("Conta", AccountType.ContaCorrente, 100m, ValidUserId);
        account.UpdateBalance(25m);

        account.Rename("Outro Nome", AccountType.Poupanca);

        Assert.Equal(100m, account.InitialBalance);
        Assert.Equal(125m, account.Balance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        var account = new Account("Conta", AccountType.ContaCorrente, 100m, ValidUserId);

        Assert.Throws<ArgumentException>(() => account.Rename(invalidName!, AccountType.Poupanca));
    }
}
