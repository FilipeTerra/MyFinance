namespace MyFinance.Application.Dtos
{
    public class ProactiveInsightResponseDto
    {
        public bool Success { get; set; }
        // Preenchido apenas quando Success = false (ex: renda não cadastrada, sessão expirada).
        public string? Message { get; set; }
        // Decisão de exibição já resolvida no backend: false = reserva adequada, não mostrar nada.
        public bool ShowCard { get; set; }
        // "aviso" (já investe, mas não atingiu o ideal — sem botão de criar meta) ou
        // "info" (ainda não iniciou — mostra o botão de criar meta).
        public string? CardType { get; set; }
        public string? Curiosity { get; set; }
        public string? Information { get; set; }
        public string? Suggestion { get; set; }
        public decimal IdealAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal MissingAmount { get; set; }
        public decimal PercentAchieved { get; set; }
    }
}
