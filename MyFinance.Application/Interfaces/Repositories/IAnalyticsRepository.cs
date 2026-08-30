using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Analytics;

namespace MyFinance.Application.Interfaces.Repositories;

/// <summary>
/// Consultas de agregação sobre transações do usuário, usadas pela análise de gastos do dashboard.
/// Todas as consultas são restritas ao usuário através da conta (<c>Transaction.Account.UserId</c>),
/// já que <see cref="MyFinance.Domain.Entities.Transaction"/> não possui UserId próprio.
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>
    /// Soma de despesas por categoria no período. O campo <see cref="CategoryExpenseDto.Percentage"/>
    /// não é preenchido aqui — cabe ao serviço calculá-lo com base no total geral.
    /// </summary>
    Task<IEnumerable<CategoryExpenseDto>> GetCategoryTotalsAsync(Guid userId, DateTime start, DateTime end, Guid? accountId);

    /// <summary>
    /// Totais de receita e despesa (e contagem de despesas) no período, sem quebra por categoria.
    /// </summary>
    Task<(decimal Expenses, decimal Income, int ExpenseCount)> GetPeriodTotalsAsync(Guid userId, DateTime start, DateTime end, Guid? accountId);

    /// <summary>
    /// Os <paramref name="take"/> maiores lançamentos individuais de despesa no período, decrescente por valor.
    /// </summary>
    Task<IEnumerable<TopExpenseDto>> GetTopExpensesAsync(Guid userId, DateTime start, DateTime end, Guid? accountId, int take);

    /// <summary>
    /// Despesas agrupadas por (ano, mês, categoria) no período. Meses sem lançamento simplesmente
    /// não aparecem — cabe ao serviço completar as lacunas.
    /// </summary>
    Task<IEnumerable<MonthlyCategoryTotalDto>> GetMonthlyCategoryTotalsAsync(Guid userId, DateTime start, DateTime end, Guid? accountId);

    /// <summary>
    /// Receitas e despesas totais agrupadas por (ano, mês) no período.
    /// </summary>
    Task<IEnumerable<MonthlyFlowDto>> GetMonthlyFlowAsync(Guid userId, DateTime start, DateTime end, Guid? accountId);
}
