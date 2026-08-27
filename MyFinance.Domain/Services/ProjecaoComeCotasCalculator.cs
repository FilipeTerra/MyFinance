using System;
using System.Collections.Generic;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Simula a evolução mês a mês de um fundo sujeito a "come-cotas" — a
    /// antecipação semestral de IR (maio/novembro, aproximada aqui como a cada 6
    /// meses simulados) sobre o rendimento acumulado desde a última retenção.
    /// O valor retido reduz a base composta dali em diante, reproduzindo o efeito
    /// real de perder parte do capital investido a cada come-cotas.
    /// </summary>
    public static class ProjecaoComeCotasCalculator
    {
        private const int PeriodicidadeMeses = 6;

        public record MesProjecaoComeCotas(int Mes, decimal ValorAcumulado, decimal TotalAportadoAcumulado, decimal JurosAcumulado, decimal ComeCotasRetidoNoMes);

        public record ResultadoProjecaoComeCotas(
            decimal ValorFinal,
            decimal TotalAportado,
            decimal TotalGanhoBruto,
            decimal TotalComeCotasRetido,
            decimal RentabilidadePercentual,
            IReadOnlyList<MesProjecaoComeCotas> Evolucao);

        /// <param name="aportesExtras">Aportes avulsos somados ao aporte mensal recorrente no mês indicado.</param>
        /// <param name="reajusteAnualPercentual">Percentual de reajuste do aporte mensal recorrente a cada 12 meses.</param>
        public static ResultadoProjecaoComeCotas Calcular(
            decimal aporteInicial, decimal aporteMensal, decimal taxaJurosAnualPercentual, int meses, decimal aliquotaComeCotas,
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
            var aportesExtrasPorMes = ProjecaoInvestimentoCalculator.AgruparAportesExtrasPorMes(aportesExtras);

            var evolucao = new List<MesProjecaoComeCotas>(meses);
            var valorAcumulado = aporteInicial;
            var totalAportado = aporteInicial;
            var totalComeCotasRetido = 0m;
            var ganhoDesdeUltimaRetencao = 0m;
            var aporteMensalAtual = aporteMensal;

            for (var mes = 1; mes <= meses; mes++)
            {
                if (mes > 1 && (mes - 1) % 12 == 0)
                    aporteMensalAtual *= 1 + reajusteAnualPercentual / 100;

                var aporteExtraDoMes = aportesExtrasPorMes.GetValueOrDefault(mes, 0m);
                var aporteTotalDoMes = aporteMensalAtual + aporteExtraDoMes;

                var valorAntesDoMes = valorAcumulado;
                valorAcumulado = valorAcumulado * (1 + taxaMensal) + aporteTotalDoMes;
                totalAportado += aporteTotalDoMes;

                var ganhoDoMes = valorAcumulado - valorAntesDoMes - aporteTotalDoMes;
                ganhoDesdeUltimaRetencao += ganhoDoMes;

                var comeCotasRetidoNoMes = 0m;
                if (mes % PeriodicidadeMeses == 0 && ganhoDesdeUltimaRetencao > 0)
                {
                    comeCotasRetidoNoMes = Math.Round(ganhoDesdeUltimaRetencao * aliquotaComeCotas / 100, 2);
                    valorAcumulado -= comeCotasRetidoNoMes;
                    totalComeCotasRetido += comeCotasRetidoNoMes;
                    ganhoDesdeUltimaRetencao = 0m;
                }

                evolucao.Add(new MesProjecaoComeCotas(
                    mes,
                    Math.Round(valorAcumulado, 2),
                    totalAportado,
                    Math.Round(valorAcumulado - totalAportado, 2),
                    comeCotasRetidoNoMes));
            }

            valorAcumulado = Math.Round(valorAcumulado, 2);
            var totalGanhoBruto = Math.Round(valorAcumulado + totalComeCotasRetido - totalAportado, 2);
            var rentabilidade = totalAportado == 0 ? 0 : Math.Round((totalGanhoBruto / totalAportado) * 100, 2);

            return new ResultadoProjecaoComeCotas(
                valorAcumulado, totalAportado, totalGanhoBruto, totalComeCotasRetido, rentabilidade, evolucao);
        }
    }
}
