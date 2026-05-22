using System;

namespace MyFinance.Application.Dtos.FinancialGoals
{
    public record CreateFinancialGoalRequestDto
    {
        public string Name { get; init; } = string.Empty;
        public decimal TargetAmount { get; init; }
        public DateTime Deadline { get; init; }
    }
}
