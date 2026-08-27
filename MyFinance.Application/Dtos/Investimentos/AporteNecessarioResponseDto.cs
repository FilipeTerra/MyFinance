namespace MyFinance.Application.Dtos.Investimentos
{
    public class AporteNecessarioResponseDto
    {
        /// <summary>Aporte mensal necessário para atingir o valor-alvo dentro do prazo informado.</summary>
        public decimal AporteMensalNecessario { get; set; }

        /// <summary>Projeção completa (com detalhamento de tributos) simulada com o aporte necessário.</summary>
        public ProjecaoInvestimentoResponseDto Projecao { get; set; } = null!;
    }
}
