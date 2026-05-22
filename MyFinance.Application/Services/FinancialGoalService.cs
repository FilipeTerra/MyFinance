using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.FinancialGoals;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Services
{
    public class FinancialGoalService : IFinancialGoalService
    {
        private readonly IFinancialGoalRepository _goalRepository;

        public FinancialGoalService(IFinancialGoalRepository goalRepository)
        {
            _goalRepository = goalRepository;
        }

        public async Task<FinancialGoalResponseDto> CreateGoalAsync(Guid userId, CreateFinancialGoalRequestDto request)
        {
            var goal = new FinancialGoal(userId, request.Name, request.TargetAmount, request.Deadline);
            await _goalRepository.AddAsync(goal);
            return MapToDto(goal);
        }

        public async Task<IEnumerable<FinancialGoalResponseDto>> GetUserGoalsAsync(Guid userId)
        {
            var goals = await _goalRepository.GetAllByUserIdAsync(userId);
            return goals.Select(MapToDto);
        }

        public async Task AddFundsToGoalAsync(Guid goalId, Guid userId, decimal amount)
        {
            var goal = await _goalRepository.GetByIdAsync(goalId);
            if (goal == null || goal.UserId != userId)
                throw new UnauthorizedAccessException("Goal not found or does not belong to user.");
            goal.AddFunds(amount);
            await _goalRepository.UpdateAsync(goal);
        }

        private static FinancialGoalResponseDto MapToDto(FinancialGoal goal)
        {
            return new FinancialGoalResponseDto
            {
                Id = goal.Id,
                Name = goal.Name,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = goal.CurrentAmount,
                Deadline = goal.Deadline,
                CreatedAt = goal.CreatedAt,
                IsCompleted = goal.IsCompleted
            };
        }
    }
}
