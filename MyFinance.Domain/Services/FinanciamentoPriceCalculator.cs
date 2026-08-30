using System;
using System.Collections.Generic;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Calcula um financiamento pelo Sistema Price (parcelas fixas): o valor
    /// pago todo mês é sempre o mesmo, mas a composição entre juros e
    /// amortização do principal muda mês a mês — mais juros no início do
    /// contrato, mais amortização no fim, já que os juros incidem sobre um
    /// saldo devedor decrescente.
    /// </summary>
    public static class FinanciamentoPriceCalculator
    {
        public record ResultadoFinanciamento(
            decimal ValorParcela,
            decimal TotalPago,
            decimal TotalJuros,
            decimal CustoEfetivoTotalPercentual,
            IReadOnlyList<ParcelaFinanciamento> Parcelas);

        /// <summary>
        /// Simula o financiamento pela Tabela Price.
        /// </summary>
        /// <param name="valorFinanciado">Principal financiado (PV), em R$.</param>
        /// <param name="taxaJurosMensalPercentual">Taxa de juros do contrato, ao mês, em % (ex.: 1.5 para 1,5% a.m.).</param>
        /// <param name="numParcelas">Número de parcelas mensais.</param>
        public static ResultadoFinanciamento Calcular(decimal valorFinanciado, decimal taxaJurosMensalPercentual, int numParcelas)
        {
            Validar(valorFinanciado, taxaJurosMensalPercentual, numParcelas);

            var i = taxaJurosMensalPercentual / 100;

            // Fórmula da Tabela Price: PMT = PV * [i * (1+i)^n] / [(1+i)^n - 1].
            // Com taxa zero a parcela é simplesmente o principal dividido igualmente.
            decimal valorParcela;
            if (i == 0)
            {
                valorParcela = valorFinanciado / numParcelas;
            }
            else
            {
                var fator = (decimal)Math.Pow((double)(1 + i), numParcelas);
                valorParcela = valorFinanciado * (i * fator) / (fator - 1);
            }
            valorParcela = Math.Round(valorParcela, 2);

            var parcelas = new List<ParcelaFinanciamento>(numParcelas);
            var saldoDevedor = valorFinanciado;

            for (var numero = 1; numero <= numParcelas; numero++)
            {
                // Os juros do mês incidem sobre o saldo devedor deixado pelo mês
                // anterior; o restante da parcela (fixa) abate o principal.
                var juros = Math.Round(saldoDevedor * i, 2);
                var amortizacao = Math.Round(valorParcela - juros, 2);
                saldoDevedor = Math.Round(saldoDevedor - amortizacao, 2);

                parcelas.Add(new ParcelaFinanciamento(numero, valorParcela, juros, amortizacao, saldoDevedor));
            }

            var totalPago = Math.Round(valorParcela * numParcelas, 2);
            var totalJuros = Math.Round(totalPago - valorFinanciado, 2);
            var custoEfetivo = valorFinanciado == 0 ? 0 : Math.Round(totalJuros / valorFinanciado * 100, 2);

            return new ResultadoFinanciamento(valorParcela, totalPago, totalJuros, custoEfetivo, parcelas);
        }

        private static void Validar(decimal valorFinanciado, decimal taxaJurosMensalPercentual, int numParcelas)
        {
            if (valorFinanciado <= 0)
                throw new ArgumentException("O valor financiado deve ser maior que zero.", nameof(valorFinanciado));

            if (taxaJurosMensalPercentual < 0)
                throw new ArgumentException("A taxa de juros mensal não pode ser negativa.", nameof(taxaJurosMensalPercentual));

            if (numParcelas <= 0)
                throw new ArgumentException("O número de parcelas deve ser maior que zero.", nameof(numParcelas));
        }
    }
}
