namespace MyFinance.Infrastructure.Integrations.Caching
{
    /// <summary>
    /// TTLs dos caches das integrações externas.
    /// </summary>
    public class CachingOptions
    {
        public const string SectionName = "ExternalServices:Cache";

        /// <summary>
        /// Cotação/histórico/indicadores. O plano gratuito do brapi entrega dado
        /// com ~30 min de atraso — cachear abaixo disso não traz informação nova.
        /// </summary>
        public int AcaoTtlMinutes { get; set; } = 15;

        /// <summary>Selic muda a cada reunião do Copom (~45 dias); IPCA é mensal.</summary>
        public int TaxasReferenciaTtlHours { get; set; } = 6;

        /// <summary>
        /// Cache negativo: evita que um ticker inválido cadastrado consuma
        /// uma requisição da cota a cada boot da API, para sempre.
        /// </summary>
        public int FalhaTtlMinutes { get; set; } = 5;
    }
}
