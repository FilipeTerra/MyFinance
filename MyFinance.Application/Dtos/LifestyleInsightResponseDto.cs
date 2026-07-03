namespace MyFinance.Application.Dtos
{
    public class LifestyleInsightResponseDto
    {
        public bool Success { get; set; }
        // Preenchido apenas quando Success = false (ex: renda não cadastrada, sessão expirada).
        public string? Message { get; set; }
        public bool Alert { get; set; }
        public string? Curiosity { get; set; }
        public string? Information { get; set; }
        public string? Suggestion { get; set; }
        public decimal? LifestylePercentOfIncome { get; set; }
        public decimal? LifestyleGrowthPercent { get; set; }
        public decimal? InvestmentGrowthPercent { get; set; }
    }
}
