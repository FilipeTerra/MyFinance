using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    public class InvestimentoResponseDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal ValorInicial { get; set; }
        public decimal ValorAtual { get; set; }
        public InvestmentType Tipo { get; set; }
        public DateTime DataCriacao { get; set; }
        public decimal RentabilidadePercentual { get; set; }
    }
}
