using Moq;
using MyFinance.Application.Dtos.FinancialGoals;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;
using MyFinance.Application.Tests.TestHelpers;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Tests.Services;

public class FinancialGoalServiceTests
{
    private readonly Mock<IFinancialGoalRepository> _goalRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly FinancialGoalService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public FinancialGoalServiceTests()
    {
        _sut = new FinancialGoalService(_goalRepository.Object, _transactionRepository.Object, _accountRepository.Object);
    }

    // ---------- CreateGoalAsync ----------

    [Fact]
    public async Task CreateGoalAsync_PersistsGoalAndReturnsDto()
    {
        var request = new CreateFinancialGoalRequestDto
        {
            Name = "Viagem",
            TargetAmount = 5000m,
            Deadline = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        FinancialGoal? saved = null;
        _goalRepository.Setup(r => r.AddAsync(It.IsAny<FinancialGoal>()))
            .Callback<FinancialGoal>(g => saved = g)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateGoalAsync(_userId, request);

        Assert.Equal("Viagem", result.Name);
        Assert.Equal(5000m, result.TargetAmount);
        Assert.Equal(0m, result.CurrentAmount);
        Assert.False(result.IsCompleted);
        Assert.NotNull(saved);
        Assert.Equal(_userId, saved!.UserId);
    }

    // ---------- GetUserGoalsAsync ----------

    [Fact]
    public async Task GetUserGoalsAsync_ReturnsMappedGoals()
    {
        var goals = new List<FinancialGoal>
        {
            new(_userId, "Meta 1", 1000m, DateTime.UtcNow),
            new(_userId, "Meta 2", 2000m, DateTime.UtcNow)
        };
        _goalRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(goals);

        var result = (await _sut.GetUserGoalsAsync(_userId)).ToList();

        Assert.Equal(2, result.Count);
    }

    // ---------- AddFundsToGoalAsync ----------

    [Fact]
    public async Task AddFundsToGoalAsync_WhenValid_AddsFundsAndUpdates()
    {
        var goal = new FinancialGoal(_userId, "Meta", 1000m, DateTime.UtcNow);
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);

        await _sut.AddFundsToGoalAsync(goal.Id, _userId, 300m);

        Assert.Equal(300m, goal.CurrentAmount);
        _goalRepository.Verify(r => r.UpdateAsync(goal), Times.Once);
    }

    [Fact]
    public async Task AddFundsToGoalAsync_WhenGoalNotFound_ThrowsUnauthorized()
    {
        _goalRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FinancialGoal)null!);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.AddFundsToGoalAsync(Guid.NewGuid(), _userId, 100m));
    }

    [Fact]
    public async Task AddFundsToGoalAsync_WhenGoalBelongsToAnotherUser_ThrowsUnauthorized()
    {
        var goal = new FinancialGoal(Guid.NewGuid(), "Meta", 1000m, DateTime.UtcNow); // outro dono
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.AddFundsToGoalAsync(goal.Id, _userId, 100m));
        _goalRepository.Verify(r => r.UpdateAsync(It.IsAny<FinancialGoal>()), Times.Never);
    }

    // ---------- DeleteGoalAsync ----------

    [Fact]
    public async Task DeleteGoalAsync_WhenValid_RestoresBalanceRemovesContributionsAndDeletesGoal()
    {
        var goal = new FinancialGoal(_userId, "Meta", 1000m, DateTime.UtcNow);
        var accountId = Guid.NewGuid();
        var account = new Account("Conta", AccountType.ContaCorrente, 1000m, _userId);
        account.UpdateBalance(-200m); // saldo 800 após dois aportes de 100

        // Dois aportes de -100 (débito) vinculados à meta
        var contributions = new List<Transaction>
        {
            new("Aporte 1", -100m, TransactionType.Investment, DateTime.UtcNow, accountId, Guid.NewGuid(), goal.Id),
            new("Aporte 2", -100m, TransactionType.Investment, DateTime.UtcNow, accountId, Guid.NewGuid(), goal.Id)
        };

        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);
        _transactionRepository.Setup(r => r.GetByFinancialGoalIdAsync(goal.Id)).ReturnsAsync(contributions);
        _accountRepository.Setup(r => r.GetByIdAsync(accountId, _userId)).ReturnsAsync(account);
        _transactionRepository.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(MockDbTransaction.Create().Object);

        await _sut.DeleteGoalAsync(goal.Id, _userId);

        Assert.Equal(1000m, account.Balance); // 800 + 100 + 100 restaurados
        _accountRepository.Verify(r => r.Update(account), Times.Once);
        _transactionRepository.Verify(r => r.Delete(It.IsAny<Transaction>()), Times.Exactly(2));
        _goalRepository.Verify(r => r.Delete(goal), Times.Once);
        _transactionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteGoalAsync_WhenGoalNotFound_ThrowsUnauthorized()
    {
        _goalRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FinancialGoal)null!);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.DeleteGoalAsync(Guid.NewGuid(), _userId));
    }

    [Fact]
    public async Task DeleteGoalAsync_WhenGoalCompleted_ThrowsInvalidOperation()
    {
        var goal = new FinancialGoal(_userId, "Meta", 1000m, DateTime.UtcNow);
        goal.AddFunds(1000m); // conclui a meta
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteGoalAsync(goal.Id, _userId));
        _goalRepository.Verify(r => r.Delete(It.IsAny<FinancialGoal>()), Times.Never);
    }
}
