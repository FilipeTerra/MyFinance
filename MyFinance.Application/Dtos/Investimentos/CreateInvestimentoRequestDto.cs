using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    public record CreateInvestimentoRequestDto
    {
        public string Nome { get; init; } = string.Empty;
        public decimal ValorInicial { get; init; }
        public InvestmentType Tipo { get; init; }
    }
}
