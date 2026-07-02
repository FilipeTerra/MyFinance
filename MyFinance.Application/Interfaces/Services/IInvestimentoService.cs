using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;

namespace MyFinance.Application.Interfaces.Services
{
    public interface IInvestimentoService
    {
        Task<InvestimentoResponseDto> CreateInvestimentoAsync(Guid userId, CreateInvestimentoRequestDto request);
        Task<IEnumerable<InvestimentoResponseDto>> GetUserInvestimentosAsync(Guid userId);
        Task<InvestimentoResponseDto> UpdateValorAtualAsync(Guid investimentoId, Guid userId, decimal novoValor);
        Task DeleteInvestimentoAsync(Guid investimentoId, Guid userId);
    }
}
