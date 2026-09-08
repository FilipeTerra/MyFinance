namespace MyFinance.Infrastructure.Integrations.AiAgent
{
    /// <summary>
    /// Configuração do microsserviço de agentes de IA (FastAPI/LangGraph).
    /// </summary>
    public class AiAgentOptions
    {
        public const string SectionName = "ExternalServices:AiAgent";

        public string BaseUrl { get; set; } = "http://127.0.0.1:8181/";

        /// <summary>
        /// Timeout alto porque o processamento de extrato passa por LLM,
        /// que pode levar minutos em modelo local.
        /// </summary>
        public int TimeoutMinutes { get; set; } = 10;

        /// <summary>
        /// Timeout do health check. Serve para descobrir em segundos que o agente
        /// está fora, em vez de esperar o timeout longo acima.
        /// </summary>
        public int HealthTimeoutSeconds { get; set; } = 3;

        /// <summary>
        /// Timeout da sugestão de categorias. Curto de propósito: é um enriquecimento
        /// opcional da importação, que segue normalmente sem ele.
        /// </summary>
        public int SuggestionTimeoutSeconds { get; set; } = 20;
    }
}
