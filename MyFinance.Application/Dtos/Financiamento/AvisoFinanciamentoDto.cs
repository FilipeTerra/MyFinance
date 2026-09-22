namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>Aviso não bloqueante sobre a simulação — o front decide cor/ícone pelo código, não pelo texto.</summary>
    public class AvisoFinanciamentoDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;

        /// <summary>"Informativo", "Atencao" ou "Critico" — nome do enum, para o front mapear sem números mágicos.</summary>
        public string Severidade { get; set; } = string.Empty;
    }
}
