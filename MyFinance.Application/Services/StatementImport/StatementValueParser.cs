using System.Globalization;
using System.Text.RegularExpressions;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Conversões de data e valor compartilhadas pelos parsers de extrato.
/// </summary>
public static partial class StatementValueParser
{
    /// <summary>
    /// Formatos aceitos, na ordem de tentativa. O padrão brasileiro vem primeiro:
    /// em "06/02/2025" a leitura correta é 6 de fevereiro.
    /// </summary>
    private static readonly string[] DateFormats =
    {
        "dd/MM/yyyy",
        "yyyy-MM-dd",
        "dd-MM-yyyy",
        "dd/MM/yy",
        "yyyy/MM/dd",
        "dd.MM.yyyy",
        "MM/dd/yyyy"
    };

    private static readonly Dictionary<string, int> MonthAbbreviations =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["jan"] = 1, ["fev"] = 2, ["mar"] = 3, ["abr"] = 4,
            ["mai"] = 5, ["jun"] = 6, ["jul"] = 7, ["ago"] = 8,
            ["set"] = 9, ["out"] = 10, ["nov"] = 11, ["dez"] = 12
        };

    /// <summary>Palavras que, no campo "Tipo" do extrato, indicam dinheiro entrando.</summary>
    private static readonly string[] CreditKeywords =
    {
        "credito", "estorno", "reembolso", "cashback", "pix recebido",
        "ted recebida", "doc recebido", "rendimento", "pagamento recebido"
    };

    /// <summary>Palavras que indicam dinheiro saindo.</summary>
    private static readonly string[] DebitKeywords =
    {
        "debito", "compra", "saque", "tarifa", "anuidade", "juros",
        "parcela", "pix enviado", "ted enviada", "doc enviado"
    };

    // Ancorada nas duas pontas: sem o $, "03 de ago. 2026 PAGAMENTO" passaria como
    // data válida e a primeira palavra da descrição seria engolida.
    [GeneratedRegex(@"^(\d{1,2})\s+de\s+([a-zA-Zç]{3,})\.?\s+(\d{4})\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex LongDateRegex();

    [GeneratedRegex(@"[^\d,.-]")]
    private static partial Regex CurrencyNoiseRegex();

    /// <summary>Converte "30/08/2026", "2026-08-30" e afins para DateTime (Utc).</summary>
    public static bool TryParseDate(string? raw, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.Trim();

        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
        {
            date = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
            return true;
        }

        return TryParseLongDate(text, out date);
    }

    /// <summary>Converte o formato por extenso da fatura em PDF: "03 de ago. 2026".</summary>
    public static bool TryParseLongDate(string? raw, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var match = LongDateRegex().Match(raw.Trim());
        if (!match.Success)
            return false;

        var monthKey = match.Groups[2].Value[..3];
        if (!MonthAbbreviations.TryGetValue(monthKey, out var month))
            return false;

        var day = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var year = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

        if (day < 1 || day > DateTime.DaysInMonth(year, month))
            return false;

        date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        return true;
    }

    /// <summary>
    /// Converte "R$ 1.076,26", "-R$ 150,00" e "+ R$ 4.657,32" em decimal.
    /// Devolve também se o número vinha negativo, já que em extrato de cartão o
    /// sinal é a informação mais confiável sobre a natureza do lançamento.
    /// </summary>
    public static bool TryParseAmount(string? raw, out decimal amount, out bool isNegative)
    {
        amount = 0m;
        isNegative = false;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.Trim();
        isNegative = HasLeadingMinus(text);

        var cleaned = CurrencyNoiseRegex().Replace(text, string.Empty)
            .Replace("-", string.Empty)
            .Replace("+", string.Empty);

        if (cleaned.Length == 0)
            return false;

        // Formato brasileiro: ponto é separador de milhar, vírgula é decimal.
        // Quando não há vírgula, o ponto pode ser o decimal (ex.: "1076.26").
        cleaned = cleaned.Contains(',')
            ? cleaned.Replace(".", string.Empty).Replace(',', '.')
            : cleaned;

        if (!decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
            return false;

        amount = Math.Abs(amount);
        return true;
    }

    /// <summary>Indica se o campo "Tipo" do extrato descreve uma entrada de dinheiro.</summary>
    public static bool IsCreditType(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return false;

        var normalized = StatementTextNormalizer.Normalize(tipo).ToLowerInvariant();
        return CreditKeywords.Any(normalized.Contains);
    }

    /// <summary>Indica se o campo "Tipo" do extrato descreve uma saída de dinheiro.</summary>
    public static bool IsDebitType(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo) || IsCreditType(tipo))
            return false;

        var normalized = StatementTextNormalizer.Normalize(tipo).ToLowerInvariant();
        return DebitKeywords.Any(normalized.Contains);
    }

    private static bool HasLeadingMinus(string text)
    {
        foreach (var c in text)
        {
            if (c == '-')
                return true;

            if (char.IsDigit(c))
                return false;
        }

        return false;
    }
}
