namespace MyFinance.Application.Dtos
{
    public class AiTransactionResponseDto
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid AccountId { get; set; }
        public Guid? CategoryId { get; set; } // Nulo se for sugestão
        public string? SuggestedCategoryName { get; set; }
        public bool IsSuggestion { get; set; }
    }
}