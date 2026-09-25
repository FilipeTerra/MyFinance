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
            .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.Nature })
            .Select(g => new CategoryExpenseDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Nature = g.Key.Nature,
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
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.CategoryId, t.Category.Name, t.Category.Nature })
            .Select(g => new MonthlyCategoryTotalDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Nature = g.Key.Nature,
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

    public async Task<IEnumerable<MonthlyInvestmentTotalDto>> GetMonthlyInvestmentTotalsAsync(Guid userId, DateTime start, DateTime end, Guid? accountId)
    {
        // Aporte não é despesa (fica de fora de GetPeriodTotalsAsync), mas sai da mesma
        // sobra mensal — a sugestão de aporte precisa dele para não tratar como livre
        // um dinheiro que o usuário já guarda.
        return await BaseQuery(userId, start, end, accountId)
            .Where(t => t.Type == TransactionType.Investment)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new MonthlyInvestmentTotalDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(t => Math.Abs(t.Amount)),
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<InstallmentRowDto>> GetInstallmentsAsync(Guid userId, Guid? accountId, DateTime notOlderThan)
    {
        // Sem o BaseQuery de propósito: aqui não há período de análise, e o filtro por
        // InstallmentTotal é o que faz esta consulta cair no índice parcial.
        return await _context.Transactions
            .Where(t => t.Account.UserId == userId
                     && t.Type == TransactionType.Expense
                     && t.InstallmentTotal != null
                     && t.InstallmentNumber != null
                     && t.Date >= notOlderThan.Date
                     && (accountId == null || t.AccountId == accountId))
            .Select(t => new InstallmentRowDto
            {
                Description = t.Description,
                Amount = Math.Abs(t.Amount),
                Date = t.Date,
                AccountId = t.AccountId,
                InstallmentNumber = t.InstallmentNumber!.Value,
                InstallmentTotal = t.InstallmentTotal!.Value,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
            })
            .ToListAsync();
    }
}
