using System;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Converte uma taxa nominal anual em taxa real anual, descontando a
    /// inflação pela equação de Fisher: (1 + nominal) / (1 + inflação) - 1.
    /// </summary>
    public static class TaxaRealCalculator
    {
        public static decimal Calcular(decimal taxaNominalAnualPercentual, decimal inflacaoAnualPercentual)
        {
            var nominal = 1 + taxaNominalAnualPercentual / 100;
            var inflacao = 1 + inflacaoAnualPercentual / 100;

            if (inflacao == 0)
                throw new ArgumentException("A inflação informada não pode resultar em fator zero.", nameof(inflacaoAnualPercentual));

            return Math.Round((nominal / inflacao - 1) * 100, 2);
        }
    }
}
