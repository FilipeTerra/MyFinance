using System;

namespace MyFinance.Domain.Entities
{
    public class FinancialGoal
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Name { get; private set; }
        public decimal TargetAmount { get; private set; }
        public decimal CurrentAmount { get; private set; }
        public DateTime Deadline { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsCompleted { get; private set; }

        public FinancialGoal(Guid userId, string name, decimal targetAmount, DateTime deadline)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Name = name;
            TargetAmount = targetAmount;
            Deadline = deadline;
            CurrentAmount = 0m;
            IsCompleted = false;
            CreatedAt = DateTime.UtcNow;
        }

        public void AddFunds(decimal amount)
        {
            CurrentAmount += amount;
            if (CurrentAmount >= TargetAmount)
            {
                IsCompleted = true;
            }
        }
    }
}