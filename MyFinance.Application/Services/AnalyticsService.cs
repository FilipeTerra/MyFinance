using MyFinance.Application.Dtos.Analytics;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private const int TopExpensesCount = 5;

    private readonly IAnalyticsRepository _analyticsRepository;

    public AnalyticsService(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<ServiceResponse<ExpenseOverviewResponseDto>> GetExpenseOverviewAsync(Guid userId, ExpenseAnalyticsFilterDto filters)
    {
        var end = (filters.EndDate ?? DateTime.UtcNow).Date;
        var start = (filters.StartDate ?? new DateTime(end.Year, end.Month, 1)).Date;

        if (start > end)
        {
            return new ServiceResponse<ExpenseOverviewResponseDto>
            {
                Success = false,
                ErrorMessage = "A data inicial não pode ser posterior à data final.",
            };
        }

        // Período anterior: mesma duração em dias, imediatamente antes do período atual.
        var durationDays = (end - start).Days + 1;
        var previousEnd = start.AddDays(-1);
        var previousStart = previousEnd.AddDays(-(durationDays - 1));

        var categoryTotals = (await _analyticsRepository.GetCategoryTotalsAsync(userId, start, end, filters.AccountId)).ToList();
        var periodTotals = await _analyticsRepository.GetPeriodTotalsAsync(userId, start, end, filters.AccountId);
        var topExpenses = (await _analyticsRepository.GetTopExpensesAsync(userId, start, end, filters.AccountId, TopExpensesCount)).ToList();

        var previousCategoryTotals = (await _analyticsRepository.GetCategoryTotalsAsync(userId, previousStart, previousEnd, filters.AccountId)).ToList();
        var previousPeriodTotals = await _analyticsRepository.GetPeriodTotalsAsync(userId, previousStart, previousEnd, filters.AccountId);

        ApplyPercentages(categoryTotals, periodTotals.Expenses);
        categoryTotals = categoryTotals.OrderByDescending(c => c.Total).ToList();

        ApplyPercentages(previousCategoryTotals, previousPeriodTotals.Expenses);
        // Mantém, sempre que possível, a mesma ordem de categorias do período atual — facilita a
        // comparação lado a lado no front-end. Categorias que só existiam no período anterior vêm depois.
        var categoryOrder = categoryTotals.Select((c, index) => (c.CategoryId, index)).ToDictionary(x => x.CategoryId, x => x.index);
        previousCategoryTotals = previousCategoryTotals
            .OrderBy(c => categoryOrder.TryGetValue(c.CategoryId, out var index) ? index : int.MaxValue)
            .ThenByDescending(c => c.Total)
            .ToList();

        var monthlyAverage = periodTotals.Expenses / MonthsCoveredBy(start, end);

        var variationAmount = periodTotals.Expenses - previousPeriodTotals.Expenses;
        var variationPercent = previousPeriodTotals.Expenses == 0m
            ? (decimal?)null
            : Math.Round(variationAmount / previousPeriodTotals.Expenses * 100, 2);

        var response = new ExpenseOverviewResponseDto
        {
            StartDate = start,
            EndDate = end,
            TotalExpenses = periodTotals.Expenses,
            TotalIncome = periodTotals.Income,
            Balance = periodTotals.Income - periodTotals.Expenses,
            TransactionCount = periodTotals.ExpenseCount,
            MonthlyAverage = Math.Round(monthlyAverage, 2),
            PreviousTotalExpenses = previousPeriodTotals.Expenses,
            VariationAmount = variationAmount,
            VariationPercent = variationPercent,
            Categories = categoryTotals,
            PreviousCategories = previousCategoryTotals,
            TopExpenses = topExpenses,
        };

        return new ServiceResponse<ExpenseOverviewResponseDto> { Data = response };
    }

    public async Task<ServiceResponse<ExpenseTimelineResponseDto>> GetExpenseTimelineAsync(Guid userId, ExpenseAnalyticsFilterDto filters)
    {
        var end = (filters.EndDate ?? DateTime.UtcNow).Date;
        var monthsCount = Math.Clamp(filters.Months <= 0 ? 12 : filters.Months, 1, 36);
        var endMonthStart = new DateTime(end.Year, end.Month, 1);
        var start = (filters.StartDate ?? endMonthStart.AddMonths(-(monthsCount - 1))).Date;

        if (start > end)
        {
            return new ServiceResponse<ExpenseTimelineResponseDto>
            {
                Success = false,
                ErrorMessage = "A data inicial não pode ser posterior à data final.",
            };
        }

        var monthlyCategoryTotals = (await _analyticsRepository.GetMonthlyCategoryTotalsAsync(userId, start, end, filters.AccountId)).ToList();
        var monthlyFlow = (await _analyticsRepository.GetMonthlyFlowAsync(userId, start, end, filters.AccountId)).ToList();

        var months = new List<MonthlyPointDto>();
        var cursor = new DateTime(start.Year, start.Month, 1);
        var lastMonth = new DateTime(end.Year, end.Month, 1);

        while (cursor <= lastMonth)
        {
            var flow = monthlyFlow.FirstOrDefault(f => f.Year == cursor.Year && f.Month == cursor.Month);
            var totalExpenses = flow?.TotalExpenses ?? 0m;
            var totalIncome = flow?.TotalIncome ?? 0m;

            var categories = monthlyCategoryTotals
                .Where(c => c.Year == cursor.Year && c.Month == cursor.Month)
                .Select(c => new CategoryExpenseDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Total = c.Total,
                    TransactionCount = c.TransactionCount,
                })
                .ToList();
            ApplyPercentages(categories, totalExpenses);
            categories = categories.OrderByDescending(c => c.Total).ToList();

            months.Add(new MonthlyPointDto
            {
                Year = cursor.Year,
                Month = cursor.Month,
                Label = cursor.ToString("yyyy-MM"),
                TotalExpenses = totalExpenses,
                TotalIncome = totalIncome,
                Balance = totalIncome - totalExpenses,
                Categories = categories,
            });

            cursor = cursor.AddMonths(1);
        }

        return new ServiceResponse<ExpenseTimelineResponseDto> { Data = new ExpenseTimelineResponseDto { Months = months } };
    }

    /// <summary>
    /// Preenche <see cref="CategoryExpenseDto.Percentage"/> com a participação de cada categoria
    /// sobre o total informado (0 quando o total é zero, para evitar divisão por zero).
    /// </summary>
    private static void ApplyPercentages(IEnumerable<CategoryExpenseDto> categories, decimal total)
    {
        foreach (var category in categories)
        {
            category.Percentage = total == 0m ? 0m : Math.Round(category.Total / total * 100, 2);
        }
    }

    /// <summary>
    /// Número de meses-calendário cobertos pelo período (mínimo 1), usado para calcular a média mensal.
    /// </summary>
    private static int MonthsCoveredBy(DateTime start, DateTime end)
    {
        var months = ((end.Year - start.Year) * 12) + end.Month - start.Month + 1;
        return Math.Max(months, 1);
    }
}
