using Moq;
using MyFinance.Application.Dtos.Analytics;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;

namespace MyFinance.Application.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly Mock<IAnalyticsRepository> _analyticsRepository = new();
    private readonly AnalyticsService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AnalyticsServiceTests()
    {
        _sut = new AnalyticsService(_analyticsRepository.Object);
    }

    // ---------- GetExpenseOverviewAsync ----------

    [Fact]
    public async Task GetExpenseOverviewAsync_CalculatesPercentagesThatSumToTotal()
    {
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();
        var categories = new List<CategoryExpenseDto>
        {
            new() { CategoryId = categoryA, CategoryName = "Mercado", Total = 300m, TransactionCount = 3 },
            new() { CategoryId = categoryB, CategoryName = "Lazer", Total = 100m, TransactionCount = 1 },
        };

        _analyticsRepository
            .Setup(r => r.GetCategoryTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(categories);
        _analyticsRepository
            .Setup(r => r.GetPeriodTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync((400m, 0m, 4));
        _analyticsRepository
            .Setup(r => r.GetTopExpensesAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<TopExpenseDto>());

        var filters = new ExpenseAnalyticsFilterDto
        {
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 31),
        };

        var result = await _sut.GetExpenseOverviewAsync(_userId, filters);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Categories.Count);
        Assert.Equal(75m, result.Data.Categories.Single(c => c.CategoryId == categoryA).Percentage);
        Assert.Equal(25m, result.Data.Categories.Single(c => c.CategoryId == categoryB).Percentage);
        Assert.Equal(100m, result.Data.Categories.Sum(c => c.Percentage));
    }

    [Fact]
    public async Task GetExpenseOverviewAsync_WhenPreviousPeriodHasNoExpenses_VariationPercentIsNull()
    {
        _analyticsRepository
            .Setup(r => r.GetCategoryTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(Enumerable.Empty<CategoryExpenseDto>());
        _analyticsRepository
            .SetupSequence(r => r.GetPeriodTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync((500m, 0m, 2))   // período atual
            .ReturnsAsync((0m, 0m, 0));    // período anterior, sem despesas
        _analyticsRepository
            .Setup(r => r.GetTopExpensesAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<TopExpenseDto>());

        var filters = new ExpenseAnalyticsFilterDto
        {
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 31),
        };

        var result = await _sut.GetExpenseOverviewAsync(_userId, filters);

        Assert.True(result.Success);
        Assert.Null(result.Data!.VariationPercent);
        Assert.Equal(500m, result.Data.VariationAmount);
        Assert.Equal(0m, result.Data.PreviousTotalExpenses);
    }

    [Fact]
    public async Task GetExpenseOverviewAsync_ComputesPreviousPeriodWindowOfSameDuration()
    {
        _analyticsRepository
            .Setup(r => r.GetCategoryTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(Enumerable.Empty<CategoryExpenseDto>());
        _analyticsRepository
            .Setup(r => r.GetPeriodTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync((0m, 0m, 0));
        _analyticsRepository
            .Setup(r => r.GetTopExpensesAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<TopExpenseDto>());

        // Período de 10 dias (1 a 10 de agosto) → o anterior deve ser os 10 dias imediatamente
        // anteriores (22 a 31 de julho).
        var start = new DateTime(2026, 8, 1);
        var end = new DateTime(2026, 8, 10);
        var expectedPreviousStart = new DateTime(2026, 7, 22);
        var expectedPreviousEnd = new DateTime(2026, 7, 31);

        await _sut.GetExpenseOverviewAsync(_userId, new ExpenseAnalyticsFilterDto { StartDate = start, EndDate = end });

        _analyticsRepository.Verify(
            r => r.GetPeriodTotalsAsync(_userId, expectedPreviousStart, expectedPreviousEnd, null),
            Times.Once);
    }

    [Fact]
    public async Task GetExpenseOverviewAsync_WhenPeriodShorterThanOneMonth_MonthlyAverageEqualsTotal()
    {
        _analyticsRepository
            .Setup(r => r.GetCategoryTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(Enumerable.Empty<CategoryExpenseDto>());
        _analyticsRepository
            .SetupSequence(r => r.GetPeriodTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync((300m, 0m, 1))
            .ReturnsAsync((0m, 0m, 0));
        _analyticsRepository
            .Setup(r => r.GetTopExpensesAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<TopExpenseDto>());

        var filters = new ExpenseAnalyticsFilterDto
        {
            StartDate = new DateTime(2026, 8, 5),
            EndDate = new DateTime(2026, 8, 15),
        };

        var result = await _sut.GetExpenseOverviewAsync(_userId, filters);

        // Período inteiro dentro de um único mês-calendário → cobre 1 mês, média == total.
        Assert.Equal(300m, result.Data!.MonthlyAverage);
    }

    [Fact]
    public async Task GetExpenseOverviewAsync_WhenStartDateAfterEndDate_ReturnsFailure()
    {
        var result = await _sut.GetExpenseOverviewAsync(_userId, new ExpenseAnalyticsFilterDto
        {
            StartDate = new DateTime(2026, 8, 31),
            EndDate = new DateTime(2026, 8, 1),
        });

        Assert.False(result.Success);
        Assert.Null(result.Data);
    }

    // ---------- GetExpenseTimelineAsync ----------

    [Fact]
    public async Task GetExpenseTimelineAsync_FillsMonthsWithoutTransactionsWithZeroInChronologicalOrder()
    {
        _analyticsRepository
            .Setup(r => r.GetMonthlyCategoryTotalsAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(Enumerable.Empty<MonthlyCategoryTotalDto>());
        _analyticsRepository
            .Setup(r => r.GetMonthlyFlowAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(new List<MonthlyFlowDto>
            {
                new() { Year = 2026, Month = 6, TotalExpenses = 100m, TotalIncome = 200m },
                // Julho/2026 sem lançamentos — não retorna nenhuma linha do repositório.
                new() { Year = 2026, Month = 8, TotalExpenses = 50m, TotalIncome = 150m },
            });

        var filters = new ExpenseAnalyticsFilterDto
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 8, 31),
        };

        var result = await _sut.GetExpenseTimelineAsync(_userId, filters);

        Assert.True(result.Success);
        var months = result.Data!.Months;
        Assert.Equal(3, months.Count);
        Assert.Equal(new[] { "2026-06", "2026-07", "2026-08" }, months.Select(m => m.Label));
        Assert.Equal(0m, months[1].TotalExpenses);
        Assert.Equal(0m, months[1].TotalIncome);
        Assert.Empty(months[1].Categories);
    }

    [Fact]
    public async Task GetExpenseTimelineAsync_WhenStartDateAfterEndDate_ReturnsFailure()
    {
        var result = await _sut.GetExpenseTimelineAsync(_userId, new ExpenseAnalyticsFilterDto
        {
            StartDate = new DateTime(2026, 8, 31),
            EndDate = new DateTime(2026, 8, 1),
        });

        Assert.False(result.Success);
        Assert.Null(result.Data);
    }
}
