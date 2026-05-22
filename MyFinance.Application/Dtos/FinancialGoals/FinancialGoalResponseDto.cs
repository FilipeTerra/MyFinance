using System;

namespace MyFinance.Application.Dtos.FinancialGoals
{
    public class FinancialGoalResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCompleted { get; set; }
        public decimal ProgressPercentage => TargetAmount == 0 ? 0 : (CurrentAmount / TargetAmount) * 100;
    }
}
