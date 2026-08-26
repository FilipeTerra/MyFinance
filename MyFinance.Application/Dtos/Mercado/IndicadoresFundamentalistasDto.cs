namespace MyFinance.Application.Dtos.Mercado
{
    /// <summary>
    /// Indicadores fundamentalistas de um ativo da B3.
    ///
    /// Todos os valores são nullable de propósito: quando o provedor não tem o
    /// indicador para aquele ativo, o campo vem null e é OMITIDO do que chega ao
    /// agente de IA — em vez de virar 0.0, que o modelo leria como fato
    /// ("payout de 0%") e repassaria ao usuário como se fosse dado real.
    /// </summary>
    public class IndicadoresFundamentalistasDto
    {
        public string Ticker { get; set; } = string.Empty;

        public decimal? PrecoAtualBrl { get; set; }
        public decimal? Minima52Semanas { get; set; }
        public decimal? Maxima52Semanas { get; set; }
        public decimal? DividendYield { get; set; }

        /// <summary>Indisponível no plano gratuito do provedor — sempre null.</summary>
        public decimal? DividendYieldMedio5Anos { get; set; }

        /// <summary>Indisponível no plano gratuito do provedor — sempre null.</summary>
        public decimal? Payout { get; set; }

        public decimal? DividaBilhoes { get; set; }
        public decimal? PL { get; set; }
        public decimal? MargemEbitda { get; set; }
        public decimal? EvEbitda { get; set; }
        public decimal? CrescimentoReceita { get; set; }
        public decimal? FluxoCaixaLivreBilhoes { get; set; }
        public decimal? ReturnOnEquity { get; set; }
        public decimal? MargemLucro { get; set; }
    }
}
