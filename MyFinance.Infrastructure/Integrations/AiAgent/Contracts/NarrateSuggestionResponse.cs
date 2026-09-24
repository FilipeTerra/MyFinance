namespace MyFinance.Infrastructure.Integrations.AiAgent.Contracts;

/// <summary>Resposta do endpoint <c>/api/ai/suggestion/narrate</c> do agente Python.</summary>
public class NarrateSuggestionResponse
{
    public bool Success { get; set; }

    /// <summary>Texto consultivo redigido pelo agente.</summary>
    public string? Texto { get; set; }

    /// <summary>Mensagem de falha, quando <see cref="Success"/> é falso.</summary>
    public string? Erro { get; set; }
}
