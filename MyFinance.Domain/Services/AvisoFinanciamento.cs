using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Um aviso não bloqueante sobre a simulação — nunca impede o cálculo, só
    /// chama atenção para algo que o número sozinho não mostra (parcela pesada
    /// pra renda, teto de valor do MCMV, etc.).
    /// </summary>
    /// <param name="Codigo">
    /// Chave estável em inglês, para o front decidir ícone/tratamento sem
    /// depender do texto da mensagem (que pode mudar).
    /// </param>
    /// <param name="Mensagem">Texto em português, pronto para exibir ao usuário.</param>
    public record AvisoFinanciamento(string Codigo, string Mensagem, SeveridadeAvisoFinanciamento Severidade);
}
