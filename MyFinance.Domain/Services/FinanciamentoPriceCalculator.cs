using System;
using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Calcula um financiamento pelo Sistema Price (parcelas fixas): o valor
    /// pago todo mês é sempre o mesmo, mas a composição entre juros e
    /// amortização do principal muda mês a mês — mais juros no início do
    /// contrato, mais amortização no fim, já que os juros incidem sobre um
    /// saldo devedor decrescente.
    ///
    /// O cronograma em si é gerado pelo <see cref="FinanciamentoCronogramaEngine"/>,
    /// compartilhado com o SAC.
    /// </summary>
    public static class FinanciamentoPriceCalculator
    {
        /// <param name="JurosSobreFinanciadoPercentual">
        /// Juros totais como percentual do valor financiado. <b>Não é o CET</b> —
        /// não considera seguros, tarifas nem o efeito do tempo sobre o dinheiro.
        /// </param>
        /// <param name="PrazoFinalMeses">Em quantos meses o contrato quitou de fato.</param>
        /// <param name="TotalDesembolsado">Tudo que sai do bolso no contrato: parcelas, extras, seguros e tarifas.</param>
        public record ResultadoFinanciamento(
            decimal ValorParcela,
            decimal TotalPago,
            decimal TotalJuros,
            decimal JurosSobreFinanciadoPercentual,
            IReadOnlyList<ParcelaFinanciamento> Parcelas,
            int PrazoFinalMeses,
            decimal TotalAmortizacaoExtra,
            decimal TotalSeguros,
            decimal TotalTaxaAdministracao,
            decimal TotalDesembolsado);

        /// <summary>
        /// Simula o financiamento pela Tabela Price.
        /// </summary>
        /// <param name="valorFinanciado">Principal financiado (PV), em R$.</param>
        /// <param name="taxaJurosMensalPercentual">Taxa de juros do contrato, ao mês, em % (ex.: 1.5 para 1,5% a.m.).</param>
        /// <param name="numParcelas">Número de parcelas mensais.</param>
        /// <param name="amortizacaoExtra">Pagamentos além da parcela. Nulo quando o mutuário paga só o contratado.</param>
        /// <param name="encargos">Seguros e tarifas cobrados por cima da parcela. Nulo quando não informados.</param>
        public static ResultadoFinanciamento Calcular(
            decimal valorFinanciado,
            decimal taxaJurosMensalPercentual,
            int numParcelas,
            AmortizacaoExtra? amortizacaoExtra = null,
            EncargosFinanciamento? encargos = null)
        {
            var cronograma = FinanciamentoCronogramaEngine.Gerar(
                SistemaAmortizacao.Price, valorFinanciado, taxaJurosMensalPercentual, numParcelas,
                amortizacaoExtra, encargos);

            var jurosSobreFinanciado = valorFinanciado == 0
                ? 0
                : Math.Round(cronograma.TotalJuros / valorFinanciado * 100, 2);

            // A parcela contratual é a do primeiro mês: a última pode ser alguns
            // centavos menor, porque é ela que liquida o resíduo de arredondamento.
            return new ResultadoFinanciamento(
                cronograma.Parcelas[0].ValorParcela,
                cronograma.TotalPago,
                cronograma.TotalJuros,
                jurosSobreFinanciado,
                cronograma.Parcelas,
                cronograma.PrazoFinalMeses,
                cronograma.TotalAmortizacaoExtra,
                cronograma.TotalSeguros,
                cronograma.TotalTaxaAdministracao,
                Math.Round(cronograma.TotalPago + cronograma.TotalSeguros + cronograma.TotalTaxaAdministracao, 2));
        }
    }
}
