using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    public record CreateInvestimentoRequestDto
    {
        public string Nome { get; init; } = string.Empty;
        public decimal ValorInicial { get; init; }
        public InvestmentType Tipo { get; init; }

        /// <summary>
        /// Conta de origem do dinheiro — será debitada no valor do aporte inicial.
        /// </summary>
        public Guid AccountId { get; init; }

        /// <summary>
        /// Categoria da transação de origem gerada pelo aporte.
        /// </summary>
        public Guid CategoryId { get; init; }
    }
}
