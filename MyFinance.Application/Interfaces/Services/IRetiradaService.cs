using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IRetiradaService
    {
        Task<RetiradaResponseDto> CalcularSaqueSustentavelAsync(CalcularSaqueSustentavelRequestDto request);
        Task<RetiradaResponseDto> CalcularDuracaoAsync(CalcularDuracaoRetiradaRequestDto request);
    }
}
