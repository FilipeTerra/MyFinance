using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Interfaces.Repositories
{
    public interface IInvestimentoRepository
    {
        Task<Investimento> GetByIdAsync(Guid id);
        Task<IEnumerable<Investimento>> GetAllByUserIdAsync(Guid userId);
        Task AddAsync(Investimento investimento);
        Task UpdateAsync(Investimento investimento);
        Task DeleteAsync(Investimento investimento);
    }
}
