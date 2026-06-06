using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Interfaces.Repositories
{
    public interface IFinancialGoalRepository
    {
        Task<FinancialGoal> GetByIdAsync(Guid id);
        Task<IEnumerable<FinancialGoal>> GetAllByUserIdAsync(Guid userId);
        Task AddAsync(FinancialGoal goal);
        Task UpdateAsync(FinancialGoal goal);
        void Delete(FinancialGoal goal);
    }
}
