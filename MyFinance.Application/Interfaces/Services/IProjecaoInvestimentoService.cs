using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IProjecaoInvestimentoService
    {
        Task<ProjecaoInvestimentoResponseDto> CalcularProjecaoAsync(CalcularProjecaoRequestDto request);
    }
}
