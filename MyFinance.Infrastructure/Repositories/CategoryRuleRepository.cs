using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;

namespace MyFinance.Infrastructure.Repositories;

public class CategoryRuleRepository : ICategoryRuleRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryRule>> GetAllByUserIdAsync(Guid userId)
    {
        return await _context.CategoryRules
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task UpsertRangeAsync(Guid userId, IReadOnlyCollection<CategoryRuleDraft> drafts)
    {
        if (drafts.Count == 0)
            return;

        var keys = drafts.Select(d => d.DescriptionKey).Distinct().ToList();

        var existing = await _context.CategoryRules
            .Where(r => r.UserId == userId && keys.Contains(r.DescriptionKey))
            .ToDictionaryAsync(r => r.DescriptionKey);

        foreach (var draft in drafts)
        {
            if (existing.TryGetValue(draft.DescriptionKey, out var rule))
            {
                rule.PointTo(draft.CategoryId);
                continue;
            }

            var created = new CategoryRule(userId, draft.DescriptionKey, draft.CategoryId);
            await _context.CategoryRules.AddAsync(created);

            // O mesmo lote pode trazer a descrição repetida; registrar aqui evita
            // uma segunda inserção que violaria o índice único.
            existing[draft.DescriptionKey] = created;
        }
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
