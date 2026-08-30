using System;
using System.Collections.Generic;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Calcula um financiamento pelo Sistema de Amortização Constante (SAC): a
    /// fatia do principal quitada a cada mês é sempre a mesma, mas os juros
    /// incidem sobre um saldo devedor decrescente — por isso, ao contrário do
    /// Sistema Price, a parcela total diminui mês a mês.
    /// </summary>
    public static class FinanciamentoSacCalculator
    {
        public record ResultadoFinanciamento(
            decimal PrimeiraParcela,
            decimal UltimaParcela,
            decimal TotalPago,
            decimal TotalJuros,
            decimal CustoEfetivoTotalPercentual,
            IReadOnlyList<ParcelaFinanciamento> Parcelas);

        /// <summary>
        /// Simula o financiamento pelo SAC.
        /// </summary>
        /// <param name="valorFinanciado">Principal financiado (PV), em R$.</param>
        /// <param name="taxaJurosMensalPercentual">Taxa de juros do contrato, ao mês, em % (ex.: 1.5 para 1,5% a.m.).</param>
        /// <param name="numParcelas">Número de parcelas mensais.</param>
        public static ResultadoFinanciamento Calcular(decimal valorFinanciado, decimal taxaJurosMensalPercentual, int numParcelas)
        {
            Validar(valorFinanciado, taxaJurosMensalPercentual, numParcelas);

            var i = taxaJurosMensalPercentual / 100;

            // No SAC a amortização é sempre o principal dividido igualmente
            // entre todas as parcelas — é isso que torna as parcelas decrescentes,
            // já que só os juros (sobre o saldo, que cai) variam mês a mês.
            var amortizacaoConstante = Math.Round(valorFinanciado / numParcelas, 2);

            var parcelas = new List<ParcelaFinanciamento>(numParcelas);
            var saldoDevedor = valorFinanciado;
            decimal totalPago = 0;

            for (var numero = 1; numero <= numParcelas; numero++)
            {
                var juros = Math.Round(saldoDevedor * i, 2);
                var valorParcela = amortizacaoConstante + juros;
                saldoDevedor = Math.Round(saldoDevedor - amortizacaoConstante, 2);

                parcelas.Add(new ParcelaFinanciamento(numero, valorParcela, juros, amortizacaoConstante, saldoDevedor));
                totalPago += valorParcela;
            }

            totalPago = Math.Round(totalPago, 2);
            var totalJuros = Math.Round(totalPago - valorFinanciado, 2);
            var custoEfetivo = valorFinanciado == 0 ? 0 : Math.Round(totalJuros / valorFinanciado * 100, 2);

            return new ResultadoFinanciamento(
                parcelas[0].ValorParcela, parcelas[^1].ValorParcela, totalPago, totalJuros, custoEfetivo, parcelas);
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
