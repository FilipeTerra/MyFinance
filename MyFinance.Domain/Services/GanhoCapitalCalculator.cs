using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Calcula o Imposto de Renda sobre ganho de capital para ativos de renda
    /// variável (ações, FII, criptomoedas). Alíquota fixa por categoria — sem
    /// tabela regressiva por prazo e sem IOF, diferente da renda fixa.
    /// </summary>
    public static class GanhoCapitalCalculator
    {
        public record ResultadoGanhoCapital(decimal AliquotaPercentual, decimal ValorImposto, decimal ValorLiquido, bool Isento);

        public static ResultadoGanhoCapital Calcular(decimal totalJuros, decimal valorFinal, CategoriaTributariaAtivo categoria)
        {
            if (totalJuros <= 0)
                return new ResultadoGanhoCapital(0m, 0m, valorFinal, true);

            var (aliquota, limiteIsencaoVendas) = categoria switch
            {
                CategoriaTributariaAtivo.GanhoCapitalAcao => (15m, 20_000m),
                CategoriaTributariaAtivo.GanhoCapitalFii => (20m, 0m),
                CategoriaTributariaAtivo.GanhoCapitalCripto => (15m, 35_000m),
                CategoriaTributariaAtivo.GanhoCapitalFundoAcoes => (15m, 0m),
                _ => throw new ArgumentException("Categoria não é de ganho de capital.", nameof(categoria))
            };

            if (limiteIsencaoVendas > 0 && valorFinal < limiteIsencaoVendas)
                return new ResultadoGanhoCapital(0m, 0m, valorFinal, true);

            var valorImposto = Math.Round(totalJuros * aliquota / 100, 2);

            return new ResultadoGanhoCapital(aliquota, valorImposto, valorFinal - valorImposto, false);
        }
    }
}
