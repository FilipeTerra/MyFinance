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
    }
}
