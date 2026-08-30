using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <param name="aportesExtras">
        /// Aportes avulsos (13º salário, bônus) somados ao aporte mensal recorrente
        /// no mês indicado. Vários aportes no mesmo mês são somados.
        /// </param>
        /// <param name="reajusteAnualPercentual">
        /// Percentual de reajuste aplicado ao aporte mensal recorrente a cada 12
        /// meses (ex.: reajuste salarial). Não afeta aportes extras nem o aporte inicial.
        /// </param>
        public static ResultadoProjecao Calcular(
            decimal aporteInicial, decimal aporteMensal, decimal taxaJurosAnualPercentual, int meses,
            IReadOnlyList<AporteExtra>? aportesExtras = null, decimal reajusteAnualPercentual = 0m)
        {
            if (aporteInicial < 0)
                throw new ArgumentException("O aporte inicial não pode ser negativo.", nameof(aporteInicial));

            if (aporteMensal < 0)
                throw new ArgumentException("O aporte mensal não pode ser negativo.", nameof(aporteMensal));

            if (taxaJurosAnualPercentual < 0)
                throw new ArgumentException("A taxa de juros anual não pode ser negativa.", nameof(taxaJurosAnualPercentual));

            if (meses <= 0)
                throw new ArgumentException("O prazo em meses deve ser maior que zero.", nameof(meses));

            if (reajusteAnualPercentual < 0)
                throw new ArgumentException("O reajuste anual não pode ser negativo.", nameof(reajusteAnualPercentual));

            var taxaMensal = (decimal)Math.Pow((double)(1 + taxaJurosAnualPercentual / 100), 1.0 / 12) - 1;
            var aportesExtrasPorMes = AgruparAportesExtrasPorMes(aportesExtras);

            var evolucao = new List<MesProjecao>(meses);
            var valorAcumulado = aporteInicial;
            var totalAportado = aporteInicial;
            var aporteMensalAtual = aporteMensal;

            for (var mes = 1; mes <= meses; mes++)
            {
                if (mes > 1 && (mes - 1) % 12 == 0)
                    aporteMensalAtual *= 1 + reajusteAnualPercentual / 100;

                var aporteExtraDoMes = aportesExtrasPorMes.GetValueOrDefault(mes, 0m);
                var aporteTotalDoMes = aporteMensalAtual + aporteExtraDoMes;

                valorAcumulado = valorAcumulado * (1 + taxaMensal) + aporteTotalDoMes;
                totalAportado += aporteTotalDoMes;

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

        internal static Dictionary<int, decimal> AgruparAportesExtrasPorMes(IReadOnlyList<AporteExtra>? aportesExtras)
        {
            if (aportesExtras is null || aportesExtras.Count == 0)
                return new Dictionary<int, decimal>();

            foreach (var aporte in aportesExtras)
            {
                if (aporte.Mes <= 0)
                    throw new ArgumentException("O mês de um aporte extra deve ser maior que zero.", nameof(aportesExtras));

                if (aporte.Valor <= 0)
                    throw new ArgumentException("O valor de um aporte extra deve ser maior que zero.", nameof(aportesExtras));
            }

            return aportesExtras
                .GroupBy(a => a.Mes)
                .ToDictionary(g => g.Key, g => g.Sum(a => a.Valor));
        }
    }
}
