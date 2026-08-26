using System;

namespace MyFinance.Domain.Entities
{
    /// <summary>
    /// Representa um ponto da série histórica de cotação de mercado de um investimento,
    /// usado para calcular variações por período (ex: últimos 3 meses) e alimentar gráficos.
    /// </summary>
    public class CotacaoHistorico
    {
        public Guid Id { get; private set; }
        public Guid InvestimentoId { get; private set; }
        public DateTime Data { get; private set; }
        public decimal Valor { get; private set; }

        public CotacaoHistorico(Guid investimentoId, DateTime data, decimal valor)
        {
            if (investimentoId == Guid.Empty)
                throw new ArgumentException("A cotação precisa pertencer a um investimento.", nameof(investimentoId));

            if (valor < 0)
                throw new ArgumentException("O valor da cotação não pode ser negativo.", nameof(valor));

            Id = Guid.NewGuid();
            InvestimentoId = investimentoId;
            Data = data;
            Valor = valor;
        }
    }
}
