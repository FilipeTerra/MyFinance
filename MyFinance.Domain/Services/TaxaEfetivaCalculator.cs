using System;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Converte uma taxa nominal anual (APR) com capitalização em <c>m</c>
    /// períodos por ano na taxa efetiva anual (EAR) correspondente:
    /// EAR = (1 + APR/m)^m − 1. Diferente do <see cref="TaxaRealCalculator"/>,
    /// que desconta a inflação — aqui o efeito é puramente o dos juros sobre
    /// juros dentro do próprio ano, causado pela frequência de capitalização.
    /// </summary>
    public static class TaxaEfetivaCalculator
    {
        public static decimal Calcular(decimal taxaNominalAnualPercentual, int capitalizacoesPorAno)
        {
            if (taxaNominalAnualPercentual < 0)
                throw new ArgumentException("A taxa nominal anual não pode ser negativa.", nameof(taxaNominalAnualPercentual));

            if (capitalizacoesPorAno <= 0)
                throw new ArgumentException("O número de capitalizações por ano deve ser maior que zero.", nameof(capitalizacoesPorAno));

            var apr = taxaNominalAnualPercentual / 100;
            var ear = Math.Pow(1 + (double)apr / capitalizacoesPorAno, capitalizacoesPorAno) - 1;

            return Math.Round((decimal)ear * 100, 4);
        }
    }
}
