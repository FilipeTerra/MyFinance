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
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAccountRepository _accountRepository;

        public FinancialGoalService(
            IFinancialGoalRepository goalRepository,
            ITransactionRepository transactionRepository,
            IAccountRepository accountRepository)
        {
            _goalRepository = goalRepository;
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
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

        public async Task DeleteGoalAsync(Guid goalId, Guid userId)
        {
            var goal = await _goalRepository.GetByIdAsync(goalId);
            if (goal == null || goal.UserId != userId)
                throw new UnauthorizedAccessException("Meta não encontrada ou não pertence ao usuário.");

            if (goal.IsCompleted)
                throw new InvalidOperationException("Não é possível excluir uma meta já concluída.");

            var contributions = (await _transactionRepository.GetByFinancialGoalIdAsync(goalId)).ToList();

            await using var dbTransaction = await _transactionRepository.BeginTransactionAsync();
            try
            {
                foreach (var group in contributions.GroupBy(t => t.AccountId))
                {
                    var account = await _accountRepository.GetByIdAsync(group.Key, userId);
                    if (account != null)
                    {
                        // Transaction.Amount é negativo (débito), então negá-lo restaura o saldo
                        var totalToRestore = group.Sum(t => -t.Amount);
                        account.UpdateBalance(totalToRestore);
                        _accountRepository.Update(account);
                    }
                }

                foreach (var contribution in contributions)
                    _transactionRepository.Delete(contribution);

                _goalRepository.Delete(goal);

                await _transactionRepository.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
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
