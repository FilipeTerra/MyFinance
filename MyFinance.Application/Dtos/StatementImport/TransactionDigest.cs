using MyFinance.Application.Services.StatementImport;

namespace MyFinance.Application.Dtos.StatementImport;

/// <summary>
/// Projeção enxuta de uma transação já salva, com o mínimo necessário para
/// reconhecê-la num extrato reimportado.
/// </summary>
public sealed record TransactionDigest(DateTime Date, decimal Amount, string Description);

/// <summary>
/// Identidade de um lançamento para fins de duplicata: mesma data, mesmo valor e
/// mesma descrição normalizada. A normalização é o que faz o mesmo gasto lido do
/// CSV e do PDF da fatura casar, apesar de o espaçamento das descrições diferir
/// entre os dois formatos.
/// </summary>
public readonly record struct TransactionFingerprint(DateTime Date, decimal Amount, string DescriptionKey)
{
    public static TransactionFingerprint For(DateTime date, decimal amount, string? description) =>
        new(date.Date, amount, StatementTextNormalizer.Normalize(description));
}
