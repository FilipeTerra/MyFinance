using System.Threading.Tasks;
using MyFinance.Application.Dtos.Financiamento;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IFinanciamentoService
    {
        Task<FinanciamentoResponseDto> SimularAsync(FinanciamentoRequestDto request);
        Task<TaxaEfetivaResponseDto> CalcularTaxaEfetivaAsync(TaxaEfetivaRequestDto request);
    }
}
