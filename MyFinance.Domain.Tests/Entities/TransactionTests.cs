using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Tests.Entities;

public class TransactionTests
{
    private static readonly Guid ValidAccountId = Guid.NewGuid();
    private static readonly Guid ValidCategoryId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesTransaction()
    {
        var date = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        var transaction = new Transaction("Salário", 1000m, TransactionType.Income, date, ValidAccountId, ValidCategoryId);

        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal("Salário", transaction.Description);
        Assert.Equal(1000m, transaction.Amount);
        Assert.Equal(TransactionType.Income, transaction.Type);
        Assert.Equal(date, transaction.Date);
        Assert.Equal(ValidAccountId, transaction.AccountId);
        Assert.Equal(ValidCategoryId, transaction.CategoryId);
        Assert.Null(transaction.FinancialGoalId);
        Assert.Null(transaction.InvestimentoId);
    }

    [Fact]
    public void Constructor_WithFinancialGoalAndInvestimentoIds_SetsOptionalFields()
    {
        var goalId = Guid.NewGuid();
        var investimentoId = Guid.NewGuid();

        var transaction = new Transaction(
            "Aporte", 200m, TransactionType.Investment, DateTime.UtcNow,
            ValidAccountId, ValidCategoryId, goalId, investimentoId);

        Assert.Equal(goalId, transaction.FinancialGoalId);
        Assert.Equal(investimentoId, transaction.InvestimentoId);
    }

    [Fact]
    public void Constructor_GeneratesDifferentIdsForEachTransaction()
    {
        var first = new Transaction("A", 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, ValidCategoryId);
        var second = new Transaction("B", 20m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, ValidCategoryId);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidDescription_ThrowsArgumentException(string? invalidDescription)
    {
        Assert.Throws<ArgumentException>(() =>
            new Transaction(invalidDescription!, 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, ValidCategoryId));
    }

    [Fact]
    public void Constructor_WithEmptyAccountId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Transaction("Descrição", 10m, TransactionType.Expense, DateTime.UtcNow, Guid.Empty, ValidCategoryId));
    }

    [Fact]
    public void Constructor_WithEmptyCategoryId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Transaction("Descrição", 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, Guid.Empty));
    }

    [Fact]
    public void Reassign_WithValidData_UpdatesEditableFields()
    {
        var transaction = new Transaction("Original", 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, ValidCategoryId);
        var newAccountId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var newDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        transaction.Reassign("Atualizada", 99m, TransactionType.Income, newDate, newAccountId, newCategoryId);

        Assert.Equal("Atualizada", transaction.Description);
        Assert.Equal(99m, transaction.Amount);
        Assert.Equal(TransactionType.Income, transaction.Type);
        Assert.Equal(newDate, transaction.Date);
        Assert.Equal(newAccountId, transaction.AccountId);
        Assert.Equal(newCategoryId, transaction.CategoryId);
    }

    [Fact]
    public void Reassign_DoesNotChangeId()
    {
        var transaction = new Transaction("Original", 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, ValidCategoryId);
        var originalId = transaction.Id;

        transaction.Reassign("Atualizada", 99m, TransactionType.Income, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(originalId, transaction.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reassign_WithInvalidDescription_ThrowsArgumentException(string? invalidDescription)
    {
        var transaction = new Transaction("Original", 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, ValidCategoryId);

        Assert.Throws<ArgumentException>(() =>
            transaction.Reassign(invalidDescription!, 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, ValidCategoryId));
    }

    [Fact]
    public void Reassign_WithEmptyAccountId_ThrowsArgumentException()
    {
        var transaction = new Transaction("Original", 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, ValidCategoryId);

        Assert.Throws<ArgumentException>(() =>
            transaction.Reassign("Descrição", 10m, TransactionType.Expense, DateTime.UtcNow, Guid.Empty, ValidCategoryId));
    }

    [Fact]
    public void Reassign_WithEmptyCategoryId_ThrowsArgumentException()
    {
        var transaction = new Transaction("Original", 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, ValidCategoryId);

        Assert.Throws<ArgumentException>(() =>
            transaction.Reassign("Descrição", 10m, TransactionType.Expense, DateTime.UtcNow, ValidAccountId, Guid.Empty));
    }
}
