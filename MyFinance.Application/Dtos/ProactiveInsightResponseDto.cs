namespace MyFinance.Application.Dtos
{
    public class ProactiveInsightResponseDto
    {
        public bool Success { get; set; }
        // Preenchido apenas quando Success = false (ex: renda não cadastrada, sessão expirada).
        public string? Message { get; set; }
        public string? Curiosity { get; set; }
        public string? Information { get; set; }
        public string? Suggestion { get; set; }
        public bool HasAdequateReserve { get; set; }
        public bool AlreadyHasReserveGoal { get; set; }
        public decimal IdealAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal MissingAmount { get; set; }
        public decimal PercentAchieved { get; set; }
    }
}
