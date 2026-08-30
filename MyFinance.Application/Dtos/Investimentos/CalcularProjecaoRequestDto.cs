using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    public record CalcularProjecaoRequestDto
    {
        public decimal AporteInicial { get; init; }
        public decimal AporteMensal { get; init; }
        public int PrazoMeses { get; init; }

        /// <summary>Aportes avulsos (13º salário, bônus) somados ao aporte mensal no mês indicado.</summary>
        public IReadOnlyList<AporteExtraDto>? AportesExtras { get; init; }

        /// <summary>Como o aporte mensal recorrente é reajustado a cada 12 meses. Padrão: sem reajuste.</summary>
        public ReajusteAporteModo ReajusteAporteModo { get; init; }

        /// <summary>
        /// Percentual de reajuste anual do aporte mensal. Obrigatório quando
        /// <see cref="ReajusteAporteModo"/> é <see cref="ReajusteAporteModo.PercentualFixo"/>.
        /// </summary>
        public decimal? ReajusteAporteAnualPercentual { get; init; }

        /// <summary>
        /// De onde vem a taxa de juros anual simulada: informada manualmente, a
        /// Selic real vigente, ou um percentual do CDI vigente.
        /// </summary>
        public FonteTaxaJuros FonteTaxaJuros { get; init; }

        /// <summary>
        /// Taxa de juros anual (%) a simular. Obrigatória quando <see cref="FonteTaxaJuros"/> é <see cref="FonteTaxaJuros.Manual"/>.
        /// </summary>
        public decimal? TaxaJurosAnualPercentual { get; init; }

        /// <summary>
        /// Percentual do CDI a simular (ex.: 110 para "110% do CDI"). Obrigatório
        /// quando <see cref="FonteTaxaJuros"/> é <see cref="FonteTaxaJuros.PercentualCdi"/>.
        /// </summary>
        public decimal? PercentualCdi { get; init; }

        /// <summary>
        /// Subtipo de ativo simulado — determina o regime de tributação (IR
        /// regressivo + IOF, isento, ou ganho de capital) aplicado ao resultado.
        /// </summary>
        public TipoAtivoCalculadora TipoAtivo { get; init; }
    }
}
