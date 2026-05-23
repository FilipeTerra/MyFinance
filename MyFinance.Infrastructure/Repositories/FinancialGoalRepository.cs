using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;

namespace MyFinance.Infrastructure.Repositories
{
    public class FinancialGoalRepository : IFinancialGoalRepository
    {
        private readonly ApplicationDbContext _context;

        public FinancialGoalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FinancialGoal> GetByIdAsync(Guid id)
        {
            return await _context.FinancialGoals.FindAsync(id);
        }

        public async Task<IEnumerable<FinancialGoal>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.FinancialGoals
                .Where(goal => goal.UserId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(FinancialGoal goal)
        {
            await _context.FinancialGoals.AddAsync(goal);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(FinancialGoal goal)
        {
            _context.FinancialGoals.Update(goal);
            await _context.SaveChangesAsync();
        }
    }
}
