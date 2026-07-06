using MyFinance.Domain.Entities;

namespace MyFinance.Domain.Tests.Entities;

public class FinancialGoalTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesGoalWithZeroCurrentAmountAndNotCompleted()
    {
        var deadline = new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var goal = new FinancialGoal(ValidUserId, "Reserva de Emergência", 10000m, deadline);

        Assert.NotEqual(Guid.Empty, goal.Id);
        Assert.Equal(ValidUserId, goal.UserId);
        Assert.Equal("Reserva de Emergência", goal.Name);
        Assert.Equal(10000m, goal.TargetAmount);
        Assert.Equal(0m, goal.CurrentAmount);
        Assert.Equal(deadline, goal.Deadline);
        Assert.False(goal.IsCompleted);
        Assert.True(goal.CreatedAt <= DateTime.UtcNow && goal.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Constructor_GeneratesDifferentIdsForEachGoal()
    {
        var first = new FinancialGoal(ValidUserId, "Meta A", 1000m, DateTime.UtcNow);
        var second = new FinancialGoal(ValidUserId, "Meta B", 1000m, DateTime.UtcNow);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void AddFunds_BelowTarget_IncreasesCurrentAmountAndKeepsNotCompleted()
    {
        var goal = new FinancialGoal(ValidUserId, "Meta", 1000m, DateTime.UtcNow);

        goal.AddFunds(400m);

        Assert.Equal(400m, goal.CurrentAmount);
        Assert.False(goal.IsCompleted);
    }

    [Fact]
    public void AddFunds_ReachingTarget_MarksGoalAsCompleted()
    {
        var goal = new FinancialGoal(ValidUserId, "Meta", 1000m, DateTime.UtcNow);

        goal.AddFunds(1000m);

        Assert.True(goal.IsCompleted);
    }

    [Fact]
    public void AddFunds_ExceedingTarget_MarksGoalAsCompleted()
    {
        var goal = new FinancialGoal(ValidUserId, "Meta", 1000m, DateTime.UtcNow);

        goal.AddFunds(1500m);

        Assert.Equal(1500m, goal.CurrentAmount);
        Assert.True(goal.IsCompleted);
    }

    [Fact]
    public void AddContribution_WithValidAmount_IncreasesCurrentAmount()
    {
        var goal = new FinancialGoal(ValidUserId, "Meta", 1000m, DateTime.UtcNow);

        goal.AddContribution(300m);

        Assert.Equal(300m, goal.CurrentAmount);
    }

    [Fact]
    public void AddContribution_ReachingTarget_MarksGoalAsCompleted()
    {
        var goal = new FinancialGoal(ValidUserId, "Meta", 1000m, DateTime.UtcNow);

        goal.AddContribution(1000m);

        Assert.True(goal.IsCompleted);
    }

    [Fact]
    public void AddContribution_AccumulatesAcrossMultipleCalls()
    {
        var goal = new FinancialGoal(ValidUserId, "Meta", 1000m, DateTime.UtcNow);

        goal.AddContribution(300m);
        goal.AddContribution(400m);

        Assert.Equal(700m, goal.CurrentAmount);
        Assert.False(goal.IsCompleted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void AddContribution_WithNonPositiveAmount_ThrowsArgumentException(double invalidAmount)
    {
        var goal = new FinancialGoal(ValidUserId, "Meta", 1000m, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => goal.AddContribution((decimal)invalidAmount));
    }
}
