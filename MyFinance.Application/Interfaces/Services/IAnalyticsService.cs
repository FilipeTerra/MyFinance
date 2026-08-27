using System;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Analytics;

namespace MyFinance.Application.Interfaces.Services;

/// <summary>
/// Análise de gastos do usuário para a aba "Gastos" do dashboard: visão geral com
/// comparação de período e evolução mensal por categoria.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Visão geral de despesas/receitas do período informado, com ranking por categoria,
    /// maiores lançamentos e comparação com o período anterior de mesma duração.
    /// </summary>
    Task<ServiceResponse<ExpenseOverviewResponseDto>> GetExpenseOverviewAsync(Guid userId, ExpenseAnalyticsFilterDto filters);

    /// <summary>
    /// Evolução mensal de despesas/receitas e composição por categoria, sem lacunas
    /// (meses sem lançamento vêm com totais zerados).
    /// </summary>
    Task<ServiceResponse<ExpenseTimelineResponseDto>> GetExpenseTimelineAsync(Guid userId, ExpenseAnalyticsFilterDto filters);
}
