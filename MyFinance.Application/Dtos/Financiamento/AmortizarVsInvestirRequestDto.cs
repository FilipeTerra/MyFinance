using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>
    /// Compara duas coisas para fazer com o mesmo dinheiro todo mês: abater a
    /// mais no financiamento, ou investir. Deliberadamente enxuto — não repete
    /// os campos de seguro/tarifa/MCMV do simulador principal, porque eles não
    /// mudam qual das duas opções compensa mais (o cálculo é sobre juros vs.
    /// rendimento líquido, e encargos incidem do mesmo jeito nos dois cenários).
    /// </summary>
    public record AmortizarVsInvestirRequestDto
    {
        public decimal ValorFinanciado { get; init; }
        public decimal TaxaJurosMensalPercentual { get; init; }
        public int NumParcelas { get; init; }

        /// <summary>Sistema de amortização do financiamento. Padrão: SAC.</summary>
        public SistemaAmortizacao Sistema { get; init; } = SistemaAmortizacao.Sac;

        /// <summary>Quanto está disponível por mês — o mesmo valor testado nos dois cenários.</summary>
        public decimal ValorDisponivelMensal { get; init; }

        /// <summary>De onde vem a taxa de juros anual do investimento: manual, Selic, ou % do CDI.</summary>
        public FonteTaxaJuros FonteTaxaJurosInvestimento { get; init; }

        /// <summary>Obrigatória quando <see cref="FonteTaxaJurosInvestimento"/> é <see cref="FonteTaxaJuros.Manual"/>.</summary>
        public decimal? TaxaJurosAnualInvestimentoPercentual { get; init; }

        /// <summary>Obrigatório quando <see cref="FonteTaxaJurosInvestimento"/> é <see cref="FonteTaxaJuros.PercentualCdi"/>.</summary>
        public decimal? PercentualCdiInvestimento { get; init; }

        /// <summary>Subtipo do ativo simulado — decide o regime de tributação do rendimento.</summary>
        public TipoAtivoCalculadora TipoAtivoInvestimento { get; init; }
    }
}
