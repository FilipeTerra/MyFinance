using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Analytics;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services.StatementImport;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Services;

/// <summary>
/// Monta o comprometido: lê as parcelas já lançadas, monta a identidade de cada compra
/// parcelada e delega a projeção a <see cref="CommittedInstallmentsCalculator"/>.
/// </summary>
public class CommittedService : ICommittedService
{
    /// <summary>
    /// Até onde olhar para trás atrás de parcelas. Cobre com folga o parcelamento comum
    /// (12x, 24x) sem arrastar compra antiga de usuário que parou de importar extrato.
    /// </summary>
    private const int MesesDeHistorico = 36;

    private readonly IAnalyticsRepository _analyticsRepository;

    public CommittedService(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<ServiceResponse<CommittedResponseDto>> GetCommittedAsync(Guid userId, Guid? accountId)
    {
        var resumo = await GetSummaryAsync(userId, accountId);

        var schedule = resumo.Schedule
            .Select(m => new CommittedMonthDto
            {
                Month = FormatarMes(m.Year, m.Month),
                Amount = m.Amount,
                PurchaseCount = m.PurchaseCount,
                ReleasedAmount = m.ReleasedVsNextMonth,
            })
            .ToList();

        var resposta = new CommittedResponseDto
        {
            TotalCommitted = resumo.TotalCommitted,
            NextMonthAmount = resumo.NextMonthAmount,
            PurchaseCount = resumo.PurchaseCount,
            LastDueMonth = resumo.Purchases.Count == 0
                ? null
                : resumo.Purchases
                    .Select(p => FormatarMes(p.LastDueYear, p.LastDueMonth))
                    .Max(StringComparer.Ordinal),
            Purchases = resumo.Purchases
                .Select(p => new CommittedPurchaseDto
                {
                    Description = LimparDescricao(p.Description),
                    CategoryId = p.CategoryId,
                    CategoryName = p.CategoryName,
                    InstallmentAmount = p.InstallmentAmount,
                    CurrentInstallment = p.CurrentInstallment,
                    TotalInstallments = p.TotalInstallments,
                    RemainingInstallments = p.RemainingInstallments,
                    RemainingAmount = p.RemainingAmount,
                    LastDueMonth = FormatarMes(p.LastDueYear, p.LastDueMonth),
                })
                .ToList(),
            Schedule = schedule,
        };

        return new ServiceResponse<CommittedResponseDto> { Data = resposta };
    }

    public async Task<CommittedInstallmentsCalculator.CommittedSummary> GetSummaryAsync(Guid userId, Guid? accountId)
    {
        var hoje = DateTime.UtcNow.Date;
        var rows = await _analyticsRepository.GetInstallmentsAsync(
            userId, accountId, hoje.AddMonths(-MesesDeHistorico));

        var entradas = rows.Select(r => new CommittedInstallmentsCalculator.InstallmentRow(
            GroupKey: MontarChave(r),
            Description: r.Description,
            Amount: r.Amount,
            Date: r.Date,
            Number: r.InstallmentNumber,
            Total: r.InstallmentTotal,
            CategoryId: r.CategoryId,
            CategoryName: r.CategoryName));

        return CommittedInstallmentsCalculator.Calculate(entradas, hoje);
    }

    /// <summary>
    /// Identidade de uma compra parcelada. A descrição normalizada já vem sem o sufixo de
    /// parcela, então todas as parcelas da mesma compra colapsam no mesmo comerciante; o
    /// total de parcelas, a conta e o valor exato da parcela separam duas compras distintas
    /// feitas na mesma loja — coincidir nos quatro é caso desprezível.
    /// </summary>
    private static string MontarChave(InstallmentRowDto row)
    {
        var merchant = StatementTextNormalizer.Normalize(row.Description);
        var centavos = (long)Math.Round(row.Amount * 100m, MidpointRounding.AwayFromZero);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{row.AccountId:N}|{row.InstallmentTotal}|{centavos}|{merchant}");
    }

    /// <summary>
    /// Tira o "(Parcela 06 de 09)" do que vai para a tela: o card já mostra a parcela num
    /// campo próprio, e repeti-la na descrição só rouba espaço na lista.
    /// </summary>
    private static string LimparDescricao(string description)
    {
        var limpa = string.Join(
            ' ', InstallmentParser.RemoveFrom(description).Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return limpa.Length == 0 ? description.Trim() : limpa;
    }

    private static string FormatarMes(int year, int month) =>
        string.Create(CultureInfo.InvariantCulture, $"{year:D4}-{month:D2}");
}
