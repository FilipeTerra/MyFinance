using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Apura o que o usuário já deve dos próximos meses por conta de compras parceladas
    /// no cartão — o "comprometido" —, a partir das parcelas que já apareceram nas faturas
    /// importadas.
    ///
    /// Cálculo puro: recebe as linhas já lidas do banco e não conhece EF, normalização de
    /// descrição nem período de análise. A chave de agrupamento (<see cref="InstallmentRow.GroupKey"/>)
    /// chega pronta, porque normalizar descrição de extrato é assunto da camada de aplicação.
    /// </summary>
    /// <remarks>
    /// A mesma compra reaparece a cada fatura com o número da parcela um a mais
    /// ("Parcela 03 de 10", depois "04 de 10"). Por isso o que interessa de cada grupo é a
    /// parcela de maior número: ela diz onde a compra está hoje e, por subtração, quanto falta.
    /// </remarks>
    public static class CommittedInstallmentsCalculator
    {
        /// <summary>Uma parcela já lançada, como veio da fatura.</summary>
        /// <param name="GroupKey">
        /// Identidade da compra parcelada — todas as parcelas da mesma compra compartilham
        /// esta chave. Montada pela camada de aplicação.
        /// </param>
        public record InstallmentRow(
            string GroupKey,
            string Description,
            decimal Amount,
            DateTime Date,
            int Number,
            int Total,
            Guid CategoryId,
            string CategoryName);

        /// <summary>Uma compra parcelada em aberto, com o que ainda falta dela.</summary>
        public record CommittedPurchase(
            string Description,
            Guid CategoryId,
            string CategoryName,
            decimal InstallmentAmount,
            int CurrentInstallment,
            int TotalInstallments,
            int RemainingInstallments,
            decimal RemainingAmount,
            int LastDueYear,
            int LastDueMonth);

        /// <summary>Quanto de parcela vence num mês futuro.</summary>
        /// <param name="ReleasedVsNextMonth">
        /// Quanto do orçamento mensal terá sido liberado até este mês, comparado ao que vence
        /// no próximo mês (o patamar de hoje). Cresce à medida que os parcelamentos terminam.
        /// </param>
        public record MonthlyCommitment(
            int Year,
            int Month,
            decimal Amount,
            int PurchaseCount,
            decimal ReleasedVsNextMonth);

        /// <summary>Retrato completo do comprometido.</summary>
        public record CommittedSummary(
            decimal TotalCommitted,
            decimal NextMonthAmount,
            int PurchaseCount,
            IReadOnlyList<CommittedPurchase> Purchases,
            IReadOnlyList<MonthlyCommitment> Schedule)
        {
            public static CommittedSummary Empty { get; } = new(
                0m, 0m, 0,
                Array.Empty<CommittedPurchase>(),
                Array.Empty<MonthlyCommitment>());
        }

        /// <summary>A parcela mais adiantada de uma compra, com o que dela ainda falta.</summary>
        private sealed record OpenPurchase(InstallmentRow Latest, int Remaining, decimal Installment);

        /// <summary>
        /// Monta o comprometido a partir das parcelas conhecidas.
        /// </summary>
        /// <param name="rows">Parcelas já lançadas, de qualquer compra e de qualquer mês.</param>
        /// <param name="referenceDate">
        /// Data de corte: só entram parcelas que ainda vão vencer depois do mês desta data.
        /// </param>
        public static CommittedSummary Calculate(IEnumerable<InstallmentRow> rows, DateTime referenceDate)
        {
            if (rows is null)
                throw new ArgumentNullException(nameof(rows));

            var referenceMonth = MonthIndex(referenceDate.Year, referenceDate.Month);
            var open = new List<OpenPurchase>();

            foreach (var group in rows.GroupBy(r => r.GroupKey, StringComparer.Ordinal))
            {
                // Maior número de parcela, e não data mais recente: reimportar uma fatura
                // antiga fora de ordem não pode fazer a compra "voltar no tempo".
                var latest = group
                    .OrderByDescending(r => r.Number)
                    .ThenByDescending(r => r.Date)
                    .First();

                var remaining = latest.Total - latest.Number;
                if (remaining <= 0)
                    continue;

                // Uma compra cuja última parcela já venceu está quitada, mesmo que o extrato
                // mais recente do usuário seja antigo e ela pareça em aberto.
                var lastDue = AddMonths(latest.Date, remaining);
                if (MonthIndex(lastDue.Year, lastDue.Month) <= referenceMonth)
                    continue;

                open.Add(new OpenPurchase(latest, remaining, Math.Round(Math.Abs(latest.Amount), 2)));
            }

            if (open.Count == 0)
                return CommittedSummary.Empty;

            var purchases = open
                .Select(p =>
                {
                    var lastDue = AddMonths(p.Latest.Date, p.Remaining);
                    return new CommittedPurchase(
                        Description: p.Latest.Description,
                        CategoryId: p.Latest.CategoryId,
                        CategoryName: p.Latest.CategoryName,
                        InstallmentAmount: p.Installment,
                        CurrentInstallment: p.Latest.Number,
                        TotalInstallments: p.Latest.Total,
                        RemainingInstallments: p.Remaining,
                        RemainingAmount: Math.Round(p.Installment * p.Remaining, 2),
                        LastDueYear: lastDue.Year,
                        LastDueMonth: lastDue.Month);
                })
                .OrderByDescending(p => p.RemainingAmount)
                .ThenBy(p => p.Description, StringComparer.Ordinal)
                .ToList();

            var schedule = BuildSchedule(open, referenceMonth);

            return new CommittedSummary(
                TotalCommitted: purchases.Sum(p => p.RemainingAmount),
                NextMonthAmount: schedule.Count > 0 ? schedule[0].Amount : 0m,
                PurchaseCount: purchases.Count,
                Purchases: purchases,
                Schedule: schedule);
        }

        /// <summary>
        /// Distribui cada parcela restante no mês em que ela vence, do próximo mês até a
        /// última parcela em aberto. Como todo parcelamento é mensal e consecutivo, a soma
        /// mensal só pode cair com o tempo — é essa queda que vira <c>ReleasedVsNextMonth</c>.
        /// </summary>
        private static List<MonthlyCommitment> BuildSchedule(IReadOnlyList<OpenPurchase> open, int referenceMonth)
        {
            var byMonth = new Dictionary<int, (decimal Amount, int Count)>();

            foreach (var purchase in open)
            {
                for (var k = 1; k <= purchase.Remaining; k++)
                {
                    var due = AddMonths(purchase.Latest.Date, k);
                    var index = MonthIndex(due.Year, due.Month);

                    // Parcelas que venceriam num mês já passado (extrato antigo importado
                    // agora) não são compromisso futuro.
                    if (index <= referenceMonth)
                        continue;

                    var current = byMonth.TryGetValue(index, out var acc) ? acc : (0m, 0);
                    byMonth[index] = (current.Item1 + purchase.Installment, current.Item2 + 1);
                }
            }

            if (byMonth.Count == 0)
                return new List<MonthlyCommitment>();

            var first = referenceMonth + 1;
            var last = byMonth.Keys.Max();
            var nextMonthAmount = byMonth.TryGetValue(first, out var head) ? head.Amount : 0m;

            var schedule = new List<MonthlyCommitment>(last - first + 2);

            // Vai até um mês depois da última parcela: é o mês em que o orçamento volta
            // inteiro, e sem ele o usuário não enxerga a data em que fica livre.
            for (var index = first; index <= last + 1; index++)
            {
                var (amount, count) = byMonth.TryGetValue(index, out var value) ? value : (0m, 0);
                schedule.Add(new MonthlyCommitment(
                    Year: index / 12,
                    Month: index % 12 + 1,
                    Amount: Math.Round(amount, 2),
                    PurchaseCount: count,
                    ReleasedVsNextMonth: Math.Round(nextMonthAmount - amount, 2)));
            }

            return schedule;
        }

        /// <summary>Mês como inteiro contínuo, para comparar e somar sem cair em fim de mês.</summary>
        private static int MonthIndex(int year, int month) => year * 12 + (month - 1);

        private static (int Year, int Month) AddMonths(DateTime date, int months)
        {
            var index = MonthIndex(date.Year, date.Month) + months;
            return (index / 12, index % 12 + 1);
        }
    }
}
