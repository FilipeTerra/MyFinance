using System;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IMetaReversaService
    {
        Task<AporteNecessarioResponseDto> CalcularAporteNecessarioAsync(CalcularAporteNecessarioRequestDto request);
        Task<PrazoNecessarioResponseDto> CalcularPrazoNecessarioAsync(CalcularPrazoNecessarioRequestDto request);
        Task<SimularMetaResponseDto> SimularMetaAsync(Guid goalId, Guid userId, SimularMetaRequestDto request);
    }
}
