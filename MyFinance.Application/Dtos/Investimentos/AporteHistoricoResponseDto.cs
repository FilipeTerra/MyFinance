using System;

namespace MyFinance.Application.Dtos.Investimentos
{
    public class AporteHistoricoResponseDto
    {
        public Guid TransactionId { get; set; }
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public string? ContaNome { get; set; }
    }
}
