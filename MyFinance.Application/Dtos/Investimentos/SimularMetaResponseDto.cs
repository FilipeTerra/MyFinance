namespace MyFinance.Application.Dtos.Investimentos
{
    public class SimularMetaResponseDto
    {
        /// <summary>Meses restantes entre hoje e o prazo da meta.</summary>
        public int PrazoMesesRestante { get; set; }

        /// <summary>
        /// Verdadeiro quando o aporte informado atinge a meta, ou sempre
        /// verdadeiro quando o aporte necessário foi calculado (por construção).
        /// </summary>
        public bool Atinge { get; set; }

        /// <summary>
        /// Aporte mensal necessário para atingir a meta — presente quando o
        /// request não informou um aporte mensal (modo "quanto preciso aportar?").
        /// </summary>
        public decimal? AporteMensalNecessario { get; set; }

        /// <summary>
        /// Diferença entre o valor final líquido projetado e o valor-alvo da meta
        /// (positivo = sobra, negativo = falta). Zero quando o aporte necessário
        /// foi calculado, já que nesse caso o valor bate por construção.
        /// </summary>
        public decimal DiferencaParaMeta { get; set; }

        public ProjecaoInvestimentoResponseDto Projecao { get; set; } = null!;
    }
}
