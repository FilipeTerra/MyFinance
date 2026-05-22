using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.FinancialGoals;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IFinancialGoalService
    {
        Task<FinancialGoalResponseDto> CreateGoalAsync(Guid userId, CreateFinancialGoalRequestDto request);
        Task<IEnumerable<FinancialGoalResponseDto>> GetUserGoalsAsync(Guid userId);
        Task AddFundsToGoalAsync(Guid goalId, Guid userId, decimal amount);
    }
}
