using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;

namespace MyFinance.Infrastructure.Repositories
{
    public class CotacaoHistoricoRepository : ICotacaoHistoricoRepository
    {
        private readonly ApplicationDbContext _context;

        public CotacaoHistoricoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(CotacaoHistorico cotacao)
        {
            await _context.CotacoesHistorico.AddAsync(cotacao);
        }

        public async Task<bool> ExisteParaDataAsync(Guid investimentoId, DateTime data)
        {
            return await _context.CotacoesHistorico
                .AnyAsync(c => c.InvestimentoId == investimentoId && c.Data.Date == data.Date);
        }

        public async Task<bool> ExisteAlgumRegistroAsync(Guid investimentoId)
        {
            return await _context.CotacoesHistorico.AnyAsync(c => c.InvestimentoId == investimentoId);
        }

        public async Task<CotacaoHistorico?> GetUltimaCotacaoAsync(Guid investimentoId)
        {
            return await _context.CotacoesHistorico
                .Where(c => c.InvestimentoId == investimentoId)
                .OrderByDescending(c => c.Data)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CotacaoHistorico>> GetByInvestimentoIdsSinceAsync(IEnumerable<Guid> investimentoIds, DateTime desde)
        {
            return await _context.CotacoesHistorico
                .Where(c => investimentoIds.Contains(c.InvestimentoId) && c.Data >= desde)
                .ToListAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
