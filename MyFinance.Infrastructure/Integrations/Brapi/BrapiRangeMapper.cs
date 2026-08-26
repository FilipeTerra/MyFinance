namespace MyFinance.Infrastructure.Integrations.Brapi
{
    /// <summary>
    /// Traduz "quantidade de meses" para os valores de `range` aceitos pelo brapi.
    /// </summary>
    internal static class BrapiRangeMapper
    {
        /// <summary>
        /// Converte meses no range correspondente, limitado ao teto do plano.
        /// Retorna também se houve clamp, para que o chamador possa logar —
        /// sem isso um pedido de 12 meses degradaria para 3 silenciosamente.
        /// </summary>
        internal static (string Range, bool Clamped) ToRange(int meses, int maxMeses)
        {
            var efetivo = Math.Max(1, Math.Min(meses, maxMeses));
            return ($"{efetivo}mo", efetivo < meses);
        }
    }
}
