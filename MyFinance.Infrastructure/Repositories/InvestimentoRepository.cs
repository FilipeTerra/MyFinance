using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;

namespace MyFinance.Infrastructure.Repositories
{
    public class InvestimentoRepository : IInvestimentoRepository
    {
        private readonly ApplicationDbContext _context;

        public InvestimentoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Investimento> GetByIdAsync(Guid id)
        {
            return await _context.Investimentos.FindAsync(id);
        }

        public async Task<IEnumerable<Investimento>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.Investimentos
                .Where(investimento => investimento.UserId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(Investimento investimento)
        {
            await _context.Investimentos.AddAsync(investimento);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Investimento investimento)
        {
            _context.Investimentos.Update(investimento);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Investimento investimento)
        {
            _context.Investimentos.Remove(investimento);
            await _context.SaveChangesAsync();
        }
    }
}
