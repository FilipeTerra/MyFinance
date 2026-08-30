using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    public class ProjecaoInvestimentoResponseDto
    {
        public decimal ValorFinal { get; set; }
        public decimal TotalAportado { get; set; }
        public decimal TotalJuros { get; set; }
        public decimal RentabilidadePercentual { get; set; }

        /// <summary>
        /// Taxa de juros anual (%) efetivamente usada no cálculo — a informada
        /// manualmente, ou a Selic real buscada via Banco Central.
        /// </summary>
        public decimal TaxaJurosAnualUtilizada { get; set; }

        /// <summary>
        /// Alíquota de IOF (%) aplicada sobre o rendimento quando o resgate
        /// simulado ocorre em menos de 30 dias corridos. Zero na maioria dos
        /// casos, já que o prazo mínimo simulado é de 1 mês.
        /// </summary>
        public decimal AliquotaIofPercentual { get; set; }

        /// <summary>Valor de IOF retido sobre o rendimento, em reais.</summary>
        public decimal ValorIof { get; set; }

        /// <summary>
        /// Alíquota de IR (%) aplicada sobre o rendimento — tabela regressiva
        /// (renda fixa, já líquido de IOF) ou alíquota fixa (ganho de capital).
        /// Zero quando o ativo é isento.
        /// </summary>
        public decimal AliquotaImpostoRendaPercentual { get; set; }

        /// <summary>Valor de IR retido sobre o rendimento, em reais.</summary>
        public decimal ValorImpostoRenda { get; set; }

        /// <summary>Valor final projetado já descontado o IOF e o IR.</summary>
        public decimal ValorFinalLiquido { get; set; }

        /// <summary>
        /// Regime de tributação resolvido a partir do <see cref="CalcularProjecaoRequestDto.TipoAtivo"/>
        /// informado — usado pelo frontend para rotular o detalhamento de tributos.
        /// </summary>
        public CategoriaTributariaAtivo CategoriaTributaria { get; set; }

        /// <summary>
        /// Verdadeiro quando o imposto de ganho de capital foi zerado pela isenção
        /// de valor de venda (ações abaixo de R$ 20.000 ou cripto abaixo de R$ 35.000).
        /// </summary>
        public bool IsentoPorFaixaDeVenda { get; set; }

        /// <summary>
        /// Alíquota (%) de come-cotas antecipada semestralmente (15% longo prazo,
        /// 20% curto prazo). Zero fora das categorias de fundo com come-cotas.
        /// </summary>
        public decimal AliquotaComeCotasPercentual { get; set; }

        /// <summary>
        /// Total já retido via come-cotas ao longo da simulação — um adiantamento
        /// do IR devido, descontado do IR complementar calculado no resgate
        /// (<see cref="ValorImpostoRenda"/>).
        /// </summary>
        public decimal ValorComeCotasRetido { get; set; }

        /// <summary>Percentual do CDI simulado (ex.: 110). Nulo fora do modo "% do CDI".</summary>
        public decimal? PercentualCdiUtilizado { get; set; }

        /// <summary>CDI anual (%) usado para derivar a taxa efetiva. Nulo fora do modo "% do CDI".</summary>
        public decimal? CdiAnualUtilizado { get; set; }

        /// <summary>
        /// IPCA anual (%) usado para calcular a rentabilidade real. Nulo quando a
        /// taxa foi informada manualmente (nesse caso não há inflação de referência
        /// disponível sem uma consulta externa).
        /// </summary>
        public decimal? IpcaAnualUtilizado { get; set; }

        /// <summary>
        /// Rentabilidade real anual (%), líquida de inflação (equação de Fisher).
        /// Nulo quando a taxa foi informada manualmente.
        /// </summary>
        public decimal? RentabilidadeRealAnualPercentual { get; set; }

        public List<MesProjecaoDto> Evolucao { get; set; } = new();
    }
}
