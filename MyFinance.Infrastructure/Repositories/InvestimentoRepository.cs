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

        public async Task<Investimento?> GetByIdAsync(Guid id)
        {
            return await _context.Investimentos.FindAsync(id);
        }

        public async Task<IEnumerable<Investimento>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.Investimentos
                .Where(investimento => investimento.UserId == userId)
                .OrderByDescending(investimento => investimento.DataCriacao)
                .ToListAsync();
        }

        public async Task AddAsync(Investimento investimento)
        {
            await _context.Investimentos.AddAsync(investimento);
        }

        public void Update(Investimento investimento)
        {
            _context.Investimentos.Update(investimento);
        }

        public void Delete(Investimento investimento)
        {
            _context.Investimentos.Remove(investimento);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
