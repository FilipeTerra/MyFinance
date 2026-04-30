namespace MyFinance.Application.Dtos
{
    public class SaveBatchTransactionRequestDto
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid AccountId { get; set; }
        
        // Se o usuário escolheu uma categoria existente
        public Guid? CategoryId { get; set; }
        
        // Se o usuário aprovou a criação de uma nova categoria sugerida
        public string? NewCategoryName { get; set; }
        public bool IsNewCategory { get; set; }
    }
}