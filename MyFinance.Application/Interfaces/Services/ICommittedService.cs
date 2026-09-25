using System;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Analytics;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Interfaces.Services;

/// <summary>
/// Apura o "comprometido": o que o usuário já deve dos próximos meses por compras
/// parceladas no cartão, a partir das parcelas que apareceram nas faturas importadas.
/// </summary>
/// <remarks>
/// Serviço próprio, e não mais um método em <see cref="IAnalyticsService"/>, porque tem
/// dois consumidores com necessidades diferentes: a aba "Gastos" quer o DTO pronto para a
/// tela, e a sugestão de aporte quer o cronograma para projetar quando a sobra do usuário
/// aumenta.
/// </remarks>
public interface ICommittedService
{
    /// <summary>Comprometido no formato da tela de gastos.</summary>
    Task<ServiceResponse<CommittedResponseDto>> GetCommittedAsync(Guid userId, Guid? accountId);

    /// <summary>
    /// Mesmo cálculo, no formato do domínio. Usado por quem precisa do cronograma mês a mês
    /// em vez do DTO da tela.
    /// </summary>
    Task<CommittedInstallmentsCalculator.CommittedSummary> GetSummaryAsync(Guid userId, Guid? accountId);
}
