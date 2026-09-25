using MyFinance.Application.Services.StatementImport;

namespace MyFinance.Application.Dtos.StatementImport;

/// <summary>
/// Projeção enxuta de uma transação já salva, com o mínimo necessário para
/// reconhecê-la num extrato reimportado.
/// </summary>
public sealed record TransactionDigest(
    DateTime Date, decimal Amount, string Description, int? InstallmentNumber = null);

/// <summary>
/// Identidade de um lançamento para fins de duplicata: mesma data, mesmo valor e
/// mesma descrição normalizada. A normalização é o que faz o mesmo gasto lido do
/// CSV e do PDF da fatura casar, apesar de o espaçamento das descrições diferir
/// entre os dois formatos.
/// </summary>
/// <param name="InstallmentNumber">
/// A descrição normalizada perde o sufixo de parcela de propósito (é o que faz todas as
/// parcelas de uma compra colapsarem na mesma categoria). Sem o número aqui, duas parcelas
/// de mesma data e mesmo valor teriam identidade igual e a segunda seria marcada como
/// duplicata que não é.
/// </param>
public readonly record struct TransactionFingerprint(
    DateTime Date, decimal Amount, string DescriptionKey, int? InstallmentNumber)
{
    public static TransactionFingerprint For(
        DateTime date, decimal amount, string? description, int? installmentNumber = null) =>
        new(date.Date, amount, StatementTextNormalizer.Normalize(description), installmentNumber);
}
