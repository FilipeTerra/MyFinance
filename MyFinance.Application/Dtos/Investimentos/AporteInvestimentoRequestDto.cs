using System;

namespace MyFinance.Application.Dtos.Investimentos
{
    public record AporteInvestimentoRequestDto
    {
        public decimal Valor { get; init; }

        /// <summary>
        /// Conta de origem do dinheiro — será debitada no valor do aporte.
        /// </summary>
        public Guid AccountId { get; init; }

        /// <summary>
        /// Categoria da transação gerada pelo aporte.
        /// </summary>
        public Guid CategoryId { get; init; }

        /// <summary>
        /// Data do aporte. Quando não informada, assume a data/hora atual (UTC).
        /// </summary>
        public DateTime? Data { get; init; }
    }
}
