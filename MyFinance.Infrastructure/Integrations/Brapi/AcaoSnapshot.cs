using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;

namespace MyFinance.Infrastructure.Integrations.Brapi
{
    /// <summary>
    /// Retrato completo de um ativo obtido em UMA única chamada ao provedor —
    /// cotação, série histórica e indicadores fundamentalistas juntos.
    ///
    /// Existe porque o brapi entrega os três numa requisição só e o plano gratuito
    /// permite apenas 1 ticker por requisição: cachear o snapshot inteiro faz com
    /// que GetHistory + GetQuote + GetIndicadores do mesmo ticker custem 1 chamada
    /// em vez de 3.
    /// </summary>
    public sealed class AcaoSnapshot
    {
        public required string Ticker { get; init; }
        public decimal? PrecoAtual { get; init; }
        public required IReadOnlyList<CotacaoPontoDto> Historico { get; init; }
        public required IndicadoresFundamentalistasDto Indicadores { get; init; }
    }
}
