namespace MyFinance.Infrastructure.Integrations.Brapi
{
    /// <summary>
    /// Configuração do provedor de dados de mercado da B3 (brapi.dev).
    /// </summary>
    public class BrapiOptions
    {
        public const string SectionName = "ExternalServices:Brapi";

        public string BaseUrl { get; set; } = "https://brapi.dev/api/";

        /// <summary>
        /// Token de acesso. NUNCA versionar: use user-secrets em desenvolvimento
        /// ou a variável de ambiente ExternalServices__Brapi__Token.
        /// Sem token, apenas PETR4, VALE3, ITUB4 e MGLU3 respondem.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// Teto de histórico do plano gratuito. Períodos maiores são clampeados
        /// para este valor (o provedor não os atende).
        /// </summary>
        public int MaxHistoryMonths { get; set; } = 3;
    }
}
