using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Dtos.Analytics;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Enums;

namespace MyFinance.Infrastructure.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly ApplicationDbContext _context;

    public AnalyticsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Base de todas as consultas de análise: transações do usuário (via conta), no período,
    /// opcionalmente restritas a uma conta. Não filtra por <see cref="Domain.Enums.TransactionType"/> —
    /// isso é responsabilidade de cada método.
    /// </summary>
    private IQueryable<Domain.Entities.Transaction> BaseQuery(Guid userId, DateTime start, DateTime end, Guid? accountId)
    {
        return _context.Transactions
            .Where(t => t.Account.UserId == userId
                     && t.Date >= start.Date
                     && t.Date <= end.Date
                     && (accountId == null || t.AccountId == accountId));
    }

    public async Task<IEnumerable<CategoryExpenseDto>> GetCategoryTotalsAsync(Guid userId, DateTime start, DateTime end, Guid? accountId)
    {
        return await BaseQuery(userId, start, end, accountId)
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.CategoryId, t.Category.Name })
            .Select(g => new CategoryExpenseDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Total = g.Sum(t => Math.Abs(t.Amount)),
                TransactionCount = g.Count(),
            })
            .ToListAsync();
    }

    public async Task<(decimal Expenses, decimal Income, int ExpenseCount)> GetPeriodTotalsAsync(Guid userId, DateTime start, DateTime end, Guid? accountId)
    {
        var totals = await BaseQuery(userId, start, end, accountId)
            .Where(t => t.Type == TransactionType.Expense || t.Type == TransactionType.Income)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount), Count = g.Count() })
            .ToListAsync();

        var expenses = totals.FirstOrDefault(t => t.Type == TransactionType.Expense);
        var income = totals.FirstOrDefault(t => t.Type == TransactionType.Income);

        return (
            Expenses: expenses is null ? 0m : Math.Abs(expenses.Total),
            Income: income?.Total ?? 0m,
            ExpenseCount: expenses?.Count ?? 0
        );
    }

    public async Task<IEnumerable<TopExpenseDto>> GetTopExpensesAsync(Guid userId, DateTime start, DateTime end, Guid? accountId, int take)
    {
        return await BaseQuery(userId, start, end, accountId)
            .Where(t => t.Type == TransactionType.Expense)
            .OrderByDescending(t => Math.Abs(t.Amount))
            .Take(take)
            .Select(t => new TopExpenseDto
            {
                Id = t.Id,
                Description = t.Description,
                Amount = Math.Abs(t.Amount),
                Date = t.Date,
                CategoryName = t.Category.Name,
                AccountName = t.Account.Name,
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<MonthlyCategoryTotalDto>> GetMonthlyCategoryTotalsAsync(Guid userId, DateTime start, DateTime end, Guid? accountId)
    {
        return await BaseQuery(userId, start, end, accountId)
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.CategoryId, t.Category.Name })
            .Select(g => new MonthlyCategoryTotalDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Total = g.Sum(t => Math.Abs(t.Amount)),
                TransactionCount = g.Count(),
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<MonthlyFlowDto>> GetMonthlyFlowAsync(Guid userId, DateTime start, DateTime end, Guid? accountId)
    {
        // Agrupa por (ano, mês, tipo) no banco; a quebra Receita x Despesa dentro de cada
        // mês é feita em memória, pois o resultado já está reduzido a poucas dezenas de linhas.
        var rows = await BaseQuery(userId, start, end, accountId)
            .Where(t => t.Type == TransactionType.Expense || t.Type == TransactionType.Income)
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Type })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Type, Total = g.Sum(t => t.Amount) })
            .ToListAsync();

        return rows
            .GroupBy(r => (r.Year, r.Month))
            .Select(g => new MonthlyFlowDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalExpenses = Math.Abs(g.Where(r => r.Type == TransactionType.Expense).Sum(r => r.Total)),
                TotalIncome = g.Where(r => r.Type == TransactionType.Income).Sum(r => r.Total),
            })
            .ToList();
    }
}
