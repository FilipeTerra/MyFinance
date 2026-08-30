using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Financiamento;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Services
{
    /// <summary>
    /// Simula um financiamento nos dois sistemas de amortização mais comuns no
    /// Brasil — Price (parcelas fixas) e SAC (amortização constante) — para o
    /// mesmo empréstimo, e aponta qual dos dois custa menos em juros totais.
    /// Também converte taxas nominais anuais (APR) em taxas efetivas (EAR).
    /// </summary>
    public class FinanciamentoService : IFinanciamentoService
    {
        public Task<FinanciamentoResponseDto> SimularAsync(FinanciamentoRequestDto request)
        {
            var price = FinanciamentoPriceCalculator.Calcular(
                request.ValorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas);
            var sac = FinanciamentoSacCalculator.Calcular(
                request.ValorFinanciado, request.TaxaJurosMensalPercentual, request.NumParcelas);

            var sistemaMaisBarato = price.TotalJuros <= sac.TotalJuros ? "Price" : "SAC";
            var diferenca = Math.Round(Math.Abs(price.TotalJuros - sac.TotalJuros), 2);

            var resposta = new FinanciamentoResponseDto
            {
                Price = new ResultadoFinanciamentoDto
                {
                    PrimeiraParcela = price.ValorParcela,
                    UltimaParcela = price.ValorParcela,
                    TotalPago = price.TotalPago,
                    TotalJuros = price.TotalJuros,
                    CustoEfetivoTotalPercentual = price.CustoEfetivoTotalPercentual,
                    Parcelas = MapearParcelas(price.Parcelas)
                },
                Sac = new ResultadoFinanciamentoDto
                {
                    PrimeiraParcela = sac.PrimeiraParcela,
                    UltimaParcela = sac.UltimaParcela,
                    TotalPago = sac.TotalPago,
                    TotalJuros = sac.TotalJuros,
                    CustoEfetivoTotalPercentual = sac.CustoEfetivoTotalPercentual,
                    Parcelas = MapearParcelas(sac.Parcelas)
                },
                SistemaMaisBarato = sistemaMaisBarato,
                DiferencaTotalJuros = diferenca
            };

            return Task.FromResult(resposta);
        }

        public Task<TaxaEfetivaResponseDto> CalcularTaxaEfetivaAsync(TaxaEfetivaRequestDto request)
        {
            var ear = TaxaEfetivaCalculator.Calcular(request.TaxaNominalAnualPercentual, request.CapitalizacoesPorAno);

            return Task.FromResult(new TaxaEfetivaResponseDto
            {
                TaxaNominalAnualPercentual = request.TaxaNominalAnualPercentual,
                CapitalizacoesPorAno = request.CapitalizacoesPorAno,
                TaxaEfetivaAnualPercentual = ear
            });
        }

        private static List<ParcelaFinanciamentoDto> MapearParcelas(IReadOnlyList<ParcelaFinanciamento> parcelas) =>
            parcelas.Select(p => new ParcelaFinanciamentoDto
            {
                Numero = p.Numero,
                ValorParcela = p.ValorParcela,
                Juros = p.Juros,
                Amortizacao = p.Amortizacao,
                SaldoDevedor = p.SaldoDevedor
            }).ToList();
    }
}
