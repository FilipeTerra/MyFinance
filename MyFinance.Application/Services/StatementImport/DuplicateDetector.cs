using MyFinance.Application.Dtos;
using MyFinance.Application.Dtos.StatementImport;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Sinaliza os lançamentos do extrato que já existem na conta — o caso de alguém
/// reimportar a mesma fatura, que ficou mais provável depois que as regras
/// aprendidas passaram a devolver a segunda importação inteira já categorizada.
///
/// Marcar, e não descartar: uma compra repetida de verdade (dois cafés no mesmo
/// dia, pelo mesmo valor, no mesmo lugar) é indistinguível de uma reimportação
/// olhando só os dados, e quem sabe a diferença é o usuário.
/// </summary>
public static class DuplicateDetector
{
    /// <summary>
    /// Marca as transações que correspondem a algo já salvo e devolve quantas foram.
    ///
    /// A comparação é por CONTAGEM, não por existência: se o arquivo traz duas
    /// linhas idênticas e o banco tem só uma, apenas a primeira é duplicata. É o
    /// que separa "reimportei o extrato" de "comprei a mesma coisa duas vezes".
    /// </summary>
    public static int Mark(
        List<AiTransactionResponseDto> transactions, IReadOnlyList<TransactionDigest> existing)
    {
        if (transactions.Count == 0 || existing.Count == 0)
            return 0;

        var remaining = new Dictionary<TransactionFingerprint, int>();
        foreach (var digest in existing)
        {
            var fingerprint = TransactionFingerprint.For(digest.Date, digest.Amount, digest.Description);
            remaining[fingerprint] = remaining.GetValueOrDefault(fingerprint) + 1;
        }

        var marked = 0;
        foreach (var transaction in transactions)
        {
            var fingerprint = TransactionFingerprint.For(
                transaction.Date, transaction.Amount, transaction.Description);

            if (remaining.GetValueOrDefault(fingerprint) <= 0)
                continue;

            remaining[fingerprint]--;
            transaction.IsDuplicate = true;
            marked++;
        }

        return marked;
    }

    /// <summary>Menor e maior data do lote, para limitar a consulta ao banco.</summary>
    public static (DateTime From, DateTime To) DateRange(IReadOnlyList<ParsedStatementEntry> entries) =>
        (entries.Min(e => e.Date).Date, entries.Max(e => e.Date).Date);
}
