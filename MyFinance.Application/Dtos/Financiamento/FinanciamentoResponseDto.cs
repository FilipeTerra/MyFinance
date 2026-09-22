using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>
    /// Resultado da simulação do mesmo empréstimo nos dois sistemas de
    /// amortização (Price e SAC), mais a comparação entre eles.
    /// </summary>
    public class FinanciamentoResponseDto
    {
        public ResultadoFinanciamentoDto Price { get; set; } = new();
        public ResultadoFinanciamentoDto Sac { get; set; } = new();

        /// <summary>Qual dos dois sistemas resulta em menos juros pagos.</summary>
        public SistemaAmortizacao SistemaMaisBarato { get; set; }

        /// <summary>Quanto a mais o sistema mais caro custa em juros totais, em R$.</summary>
        public decimal DiferencaTotalJuros { get; set; }

        /// <summary>Decomposição do imóvel entre entrada e valor financiado.</summary>
        public ComposicaoFinanciamentoDto Composicao { get; set; } = new();

        /// <summary>
        /// Avisos não bloqueantes sobre a simulação — ex.: parcela compromete mais
        /// de 30% da renda informada. Nunca impede a simulação de rodar.
        /// </summary>
        public List<AvisoFinanciamentoDto> Avisos { get; set; } = new();

        /// <summary>
        /// Faixa do MCMV resolvida pela renda informada. Nulo quando
        /// <see cref="Dtos.Financiamento.FinanciamentoRequestDto.MinhaCasaMinhaVida"/>
        /// é falso, ou quando a renda está fora de todas as faixas.
        /// </summary>
        public FaixaMcmvDto? FaixaMcmv { get; set; }

        /// <summary>
        /// Mês de referência dos valores do MCMV usados nesta simulação. Nulo
        /// quando o MCMV não foi ligado — presente mesmo sem faixa resolvida,
        /// para o rodapé da UI sempre poder avisar a idade do dado.
        /// </summary>
        public string? VigenciaReferenciaMcmv { get; set; }
    }
}
