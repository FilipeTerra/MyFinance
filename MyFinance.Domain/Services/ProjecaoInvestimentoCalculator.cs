using System;
using System.Collections.Generic;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Calcula a projeção de um investimento a longo prazo com aportes mensais
    /// constantes, usando juros compostos. Não depende de infraestrutura nem
    /// de fontes externas de taxa — recebe a taxa de juros já resolvida.
    /// </summary>
    public static class ProjecaoInvestimentoCalculator
    {
        public record MesProjecao(int Mes, decimal ValorAcumulado, decimal TotalAportadoAcumulado, decimal JurosAcumulado);

        public record ResultadoProjecao(
            decimal ValorFinal,
            decimal TotalAportado,
            decimal TotalJuros,
            decimal RentabilidadePercentual,
            IReadOnlyList<MesProjecao> Evolucao);

        /// <summary>
        /// Simula a evolução mês a mês de um investimento com aporte inicial e
        /// aportes mensais constantes, aplicando a taxa de juros anual convertida
        /// para taxa mensal equivalente (juros compostos).
        /// </summary>
        public static ResultadoProjecao Calcular(decimal aporteInicial, decimal aporteMensal, decimal taxaJurosAnualPercentual, int meses)
        {
            if (aporteInicial < 0)
                throw new ArgumentException("O aporte inicial não pode ser negativo.", nameof(aporteInicial));

            if (aporteMensal < 0)
                throw new ArgumentException("O aporte mensal não pode ser negativo.", nameof(aporteMensal));

            if (taxaJurosAnualPercentual < 0)
                throw new ArgumentException("A taxa de juros anual não pode ser negativa.", nameof(taxaJurosAnualPercentual));

            if (meses <= 0)
                throw new ArgumentException("O prazo em meses deve ser maior que zero.", nameof(meses));

            var taxaMensal = (decimal)Math.Pow((double)(1 + taxaJurosAnualPercentual / 100), 1.0 / 12) - 1;

            var evolucao = new List<MesProjecao>(meses);
            var valorAcumulado = aporteInicial;
            var totalAportado = aporteInicial;

            for (var mes = 1; mes <= meses; mes++)
            {
                valorAcumulado = valorAcumulado * (1 + taxaMensal) + aporteMensal;
                totalAportado += aporteMensal;

                evolucao.Add(new MesProjecao(
                    mes,
                    Math.Round(valorAcumulado, 2),
                    totalAportado,
                    Math.Round(valorAcumulado - totalAportado, 2)));
            }

            var totalJuros = Math.Round(valorAcumulado - totalAportado, 2);
            var rentabilidade = totalAportado == 0 ? 0 : Math.Round((totalJuros / totalAportado) * 100, 2);

            return new ResultadoProjecao(
                Math.Round(valorAcumulado, 2),
                totalAportado,
                totalJuros,
                rentabilidade,
                evolucao);
        }
    }
}
