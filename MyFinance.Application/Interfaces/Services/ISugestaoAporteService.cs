using System;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Sugestao;

namespace MyFinance.Application.Interfaces.Services;

/// <summary>
/// Cruza o aporte exigido por uma meta reversa com o orçamento real do usuário e
/// devolve um diagnóstico de comportamento financeiro: se cabe, quanto falta,
/// de onde cortar e que alternativas existem quando não cabe.
/// </summary>
public interface ISugestaoAporteService
{
    /// <summary>
    /// Monta a sugestão para o usuário informado.
    /// </summary>
    /// <param name="userId">Usuário autenticado dono do orçamento analisado.</param>
    /// <param name="request">Parâmetros da meta reversa já calculada.</param>
    Task<ServiceResponse<SugestaoAporteResponseDto>> ObterSugestaoAsync(Guid userId, SugestaoAporteRequestDto request);
}
