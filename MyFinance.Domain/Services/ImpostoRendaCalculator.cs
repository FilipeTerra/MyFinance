using System;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Calcula o Imposto de Renda sobre o rendimento de investimentos de renda fixa
    /// tributáveis (CDB, Tesouro Direto, fundos DI/RF), usando a tabela regressiva
    /// vigente. Não se aplica a ativos isentos (LCI, LCA, poupança, debêntures
    /// incentivadas) — nesse caso o chamador não deve invocar este cálculo.
    /// </summary>
    public static class ImpostoRendaCalculator
    {
        public record ResultadoImpostoRenda(decimal AliquotaPercentual, decimal ValorImposto, decimal ValorLiquido);

        /// <summary>
        /// Alíquota da tabela regressiva de IR para renda fixa, de acordo com o
        /// prazo total da aplicação (Lei 11.033/2004):
        /// até 180 dias (~6 meses): 22,5% · até 360 dias (~12 meses): 20% ·
        /// até 720 dias (~24 meses): 17,5% · acima de 720 dias: 15%.
        /// </summary>
        public static decimal ObterAliquotaRegressiva(int prazoMeses)
        {
            if (prazoMeses <= 6) return 22.5m;
            if (prazoMeses <= 12) return 20m;
            if (prazoMeses <= 24) return 17.5m;
            return 15m;
        }

        /// <summary>
        /// Aplica o IR regressivo sobre o rendimento (juros), preservando o
        /// principal aportado. Retorna alíquota zero quando o investimento é
        /// isento ou não há rendimento a tributar.
        /// </summary>
        public static ResultadoImpostoRenda Calcular(decimal totalJuros, decimal valorFinal, int prazoMeses, bool isento)
        {
            if (isento || totalJuros <= 0)
                return new ResultadoImpostoRenda(0m, 0m, valorFinal);

            var aliquota = ObterAliquotaRegressiva(prazoMeses);
            var valorImposto = Math.Round(totalJuros * aliquota / 100, 2);

            return new ResultadoImpostoRenda(aliquota, valorImposto, valorFinal - valorImposto);
        }
    }
}
