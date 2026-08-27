using System;
using System.ComponentModel.DataAnnotations;

namespace MyFinance.Application.Dtos.Analytics;

/// <summary>
/// Parâmetros de filtro comuns às consultas de análise de gastos.
/// </summary>
public class ExpenseAnalyticsFilterDto
{
    /// <summary>
    /// Início do período analisado. Quando omitido, o serviço assume o início do mês corrente.
    /// </summary>
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Fim do período analisado. Quando omitido, o serviço assume a data de hoje.
    /// </summary>
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Restringe a análise a uma única conta. Quando nulo, considera todas as contas do usuário.
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>
    /// Número de meses retroativos considerados na linha do tempo. Usado apenas pelo endpoint de timeline.
    /// </summary>
    [Range(1, 36, ErrorMessage = "O número de meses deve estar entre 1 e 36.")]
    public int Months { get; set; } = 12;
}
