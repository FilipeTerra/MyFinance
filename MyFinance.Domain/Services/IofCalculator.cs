using System;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Calcula o IOF regressivo sobre o rendimento de aplicações financeiras
    /// resgatadas em menos de 30 dias corridos (Decreto 6.306/2007, Anexo).
    /// A partir do 30º dia a alíquota é zero.
    /// </summary>
    public static class IofCalculator
    {
        public record ResultadoIof(decimal AliquotaPercentual, decimal ValorIof, decimal RendimentoLiquido);

        /// <summary>Alíquota (%) por dia corrido decorrido, para os dias 1 a 29.</summary>
        private static readonly decimal[] TabelaRegressiva =
        {
            96, 93, 90, 86, 83, 80, 76, 73, 70, 66,
            63, 60, 56, 53, 50, 46, 43, 40, 36, 33,
            30, 26, 23, 20, 16, 13, 10, 6, 3
        };

        public static decimal ObterAliquotaRegressiva(int diasCorridos)
        {
            if (diasCorridos <= 0) return TabelaRegressiva[0];
            if (diasCorridos >= 30) return 0m;

            return TabelaRegressiva[diasCorridos - 1];
        }

        /// <summary>
        /// Aplica o IOF regressivo sobre o rendimento (juros). Retorna alíquota
        /// zero quando não há rendimento a tributar ou o resgate ocorre a
        /// partir do 30º dia corrido.
        /// </summary>
        public static ResultadoIof Calcular(decimal totalJuros, int diasCorridos)
        {
            if (totalJuros <= 0)
                return new ResultadoIof(0m, 0m, totalJuros);

            var aliquota = ObterAliquotaRegressiva(diasCorridos);
            if (aliquota <= 0)
                return new ResultadoIof(0m, 0m, totalJuros);

            var valorIof = Math.Round(totalJuros * aliquota / 100, 2);

            return new ResultadoIof(aliquota, valorIof, totalJuros - valorIof);
        }
    }
}
