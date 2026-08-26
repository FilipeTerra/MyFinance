using System;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Services
{
    public class ProjecaoInvestimentoService : IProjecaoInvestimentoService
    {
        private readonly ITaxasReferenciaIntegrationService _taxasReferenciaService;

        public ProjecaoInvestimentoService(ITaxasReferenciaIntegrationService taxasReferenciaService)
        {
            _taxasReferenciaService = taxasReferenciaService;
        }

        public async Task<ProjecaoInvestimentoResponseDto> CalcularProjecaoAsync(CalcularProjecaoRequestDto request)
        {
            decimal taxa;

            if (request.UsarTaxaSelic)
            {
                var taxas = await _taxasReferenciaService.GetTaxasReferenciaAsync();
                if (taxas == null)
                    throw new InvalidOperationException(
                        "Não foi possível obter a taxa Selic no momento. Informe uma taxa manualmente ou tente novamente mais tarde.");

                taxa = taxas.SelicAnualPct;
            }
            else
            {
                if (request.TaxaJurosAnualPercentual is null)
                    throw new ArgumentException("Informe a taxa de juros anual.", nameof(request.TaxaJurosAnualPercentual));

                taxa = request.TaxaJurosAnualPercentual.Value;
            }

            var resultado = ProjecaoInvestimentoCalculator.Calcular(
                request.AporteInicial, request.AporteMensal, taxa, request.PrazoMeses);

            return new ProjecaoInvestimentoResponseDto
            {
                ValorFinal = resultado.ValorFinal,
                TotalAportado = resultado.TotalAportado,
                TotalJuros = resultado.TotalJuros,
                RentabilidadePercentual = resultado.RentabilidadePercentual,
                TaxaJurosAnualUtilizada = taxa,
                Evolucao = resultado.Evolucao.Select(m => new MesProjecaoDto
                {
                    Mes = m.Mes,
                    ValorAcumulado = m.ValorAcumulado,
                    TotalAportadoAcumulado = m.TotalAportadoAcumulado,
                    JurosAcumulado = m.JurosAcumulado
                }).ToList()
            };
        }
    }
}
