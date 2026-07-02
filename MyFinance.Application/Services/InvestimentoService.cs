using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Services
{
    public class InvestimentoService : IInvestimentoService
    {
        private readonly IInvestimentoRepository _investimentoRepository;

        public InvestimentoService(IInvestimentoRepository investimentoRepository)
        {
            _investimentoRepository = investimentoRepository;
        }

        public async Task<InvestimentoResponseDto> CreateInvestimentoAsync(Guid userId, CreateInvestimentoRequestDto request)
        {
            var investimento = new Investimento(userId, request.Nome, request.ValorInicial, request.Tipo);
            await _investimentoRepository.AddAsync(investimento);
            return MapToDto(investimento);
        }

        public async Task<IEnumerable<InvestimentoResponseDto>> GetUserInvestimentosAsync(Guid userId)
        {
            var investimentos = await _investimentoRepository.GetAllByUserIdAsync(userId);
            return investimentos.Select(MapToDto);
        }

        public async Task<InvestimentoResponseDto> UpdateValorAtualAsync(Guid investimentoId, Guid userId, decimal novoValor)
        {
            var investimento = await _investimentoRepository.GetByIdAsync(investimentoId);
            if (investimento == null || investimento.UserId != userId)
                throw new UnauthorizedAccessException("Investimento não encontrado ou não pertence ao usuário.");

            investimento.AtualizarValorAtual(novoValor);
            await _investimentoRepository.UpdateAsync(investimento);
            return MapToDto(investimento);
        }

        public async Task DeleteInvestimentoAsync(Guid investimentoId, Guid userId)
        {
            var investimento = await _investimentoRepository.GetByIdAsync(investimentoId);
            if (investimento == null || investimento.UserId != userId)
                throw new UnauthorizedAccessException("Investimento não encontrado ou não pertence ao usuário.");

            await _investimentoRepository.DeleteAsync(investimento);
        }

        private static InvestimentoResponseDto MapToDto(Investimento investimento)
        {
            return new InvestimentoResponseDto
            {
                Id = investimento.Id,
                Nome = investimento.Nome,
                ValorInicial = investimento.ValorInicial,
                ValorAtual = investimento.ValorAtual,
                Tipo = investimento.Tipo,
                DataCriacao = investimento.DataCriacao,
                RentabilidadePercentual = investimento.RentabilidadePercentual
            };
        }
    }
}
