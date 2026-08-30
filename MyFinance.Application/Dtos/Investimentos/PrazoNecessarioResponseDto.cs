namespace MyFinance.Application.Dtos.Investimentos
{
    public class PrazoNecessarioResponseDto
    {
        /// <summary>
        /// Falso quando o valor-alvo não é atingível dentro do limite de busca
        /// (50 anos) com o aporte informado — ex.: aporte mensal zero e taxa zero.
        /// </summary>
        public bool Atingivel { get; set; }

        /// <summary>Prazo mínimo, em meses, necessário para atingir o valor-alvo. Nulo quando não atingível.</summary>
        public int? PrazoMesesNecessario { get; set; }

        /// <summary>Projeção completa simulada com o prazo necessário. Nula quando não atingível.</summary>
        public ProjecaoInvestimentoResponseDto? Projecao { get; set; }
    }
}
