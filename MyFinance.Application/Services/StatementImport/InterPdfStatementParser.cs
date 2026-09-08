using System.Text.RegularExpressions;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Parser da fatura de cartão do Banco Inter em PDF.
///
/// O documento tem duas partes: as páginas iniciais de resumo e simulação de
/// parcelamento — que também contêm valores em reais e não podem ser lidas como
/// transação — e, a partir do marcador "Despesas da fatura", tabelas por cartão
/// no formato "03 de ago. 2026 | IFD*SG COMERCIO E VARE | - | R$ 27,12".
/// Só o que vem depois do marcador é considerado.
/// </summary>
public sealed partial class InterPdfStatementParser : IStatementParser
{
    private const string ExpensesMarker = "DESPESAS DA FATURA";
    private const double ColumnTolerance = 2.0;

    private readonly IPdfTextExtractor _pdfTextExtractor;

    public InterPdfStatementParser(IPdfTextExtractor pdfTextExtractor)
    {
        _pdfTextExtractor = pdfTextExtractor;
    }

    public string Name => "Inter PDF";

    public int Priority => 20;

    [GeneratedRegex(@"^R\$\s*[\d.]*\d,\d{2}$")]
    private static partial Regex AmountRegex();

    public bool CanParse(StatementFile file) => file.IsPdf;

    public IReadOnlyList<ParsedStatementEntry> Parse(StatementFile file)
    {
        var lines = _pdfTextExtractor.ExtractLines(file.Content);
        var start = lines
            .Select((line, index) => (line, index))
            .FirstOrDefault(x => Contains(x.line.Text, ExpensesMarker));

        if (start.line is null)
            return Array.Empty<ParsedStatementEntry>();

        var entries = new List<ParsedStatementEntry>();
        var beneficiaryColumnByPage = new Dictionary<int, double>();

        foreach (var line in lines.Skip(start.index))
        {
            if (TryReadTableHeader(line, out var beneficiaryLeft))
            {
                beneficiaryColumnByPage[line.PageNumber] = beneficiaryLeft;
                continue;
            }

            if (IsNoise(line.Text))
                continue;

            beneficiaryColumnByPage.TryGetValue(line.PageNumber, out var column);
            if (TryParseTransaction(line, column, out var entry))
                entries.Add(entry);
        }

        return entries;
    }

    private static bool TryParseTransaction(PdfLine line, double beneficiaryLeft, out ParsedStatementEntry entry)
    {
        entry = null!;
        var words = line.Words;

        if (!TryTakeDate(words, out var date, out var dateWordCount))
            return false;

        if (!TryTakeAmount(words, out var amount, out var isCredit, out var amountWordCount))
            return false;

        var middle = words
            .Skip(dateWordCount)
            .Take(words.Count - dateWordCount - amountWordCount)
            .ToList();

        // Quando o X do cabeçalho "Beneficiário" é conhecido, tudo à direita dele
        // é beneficiário, não descrição. Sem ele, resta limpar o "-" que o Inter
        // usa para beneficiário vazio.
        if (beneficiaryLeft > 0)
            middle = middle.Where(w => w.Right <= beneficiaryLeft + ColumnTolerance).ToList();

        while (middle.Count > 0 && middle[^1].Text is "-" or "–")
            middle.RemoveAt(middle.Count - 1);

        var description = string.Join(" ", middle.Select(w => w.Text)).Trim();
        if (description.Length == 0)
            return false;

        entry = new ParsedStatementEntry(date, description, isCredit ? amount : -amount);
        return true;
    }

    /// <summary>
    /// A data ocupa as primeiras palavras da linha ("03 de ago. 2026"), mas a
    /// quantidade exata varia com a extração, então testa-se do maior para o menor.
    /// </summary>
    private static bool TryTakeDate(IReadOnlyList<PdfWord> words, out DateTime date, out int wordCount)
    {
        date = default;
        wordCount = 0;

        for (var count = Math.Min(5, words.Count); count >= 3; count--)
        {
            var candidate = string.Join(" ", words.Take(count).Select(w => w.Text));
            if (StatementValueParser.TryParseLongDate(candidate, out date))
            {
                wordCount = count;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// O valor fecha a linha, sempre em duas palavras: "R$" e o número. Um "+"
    /// imediatamente antes marca crédito — pagamento da fatura ou estorno.
    /// Cuidado com o "-" que também aparece nessa posição: ele é o placeholder da
    /// coluna Beneficiário vazia, e lê-lo como sinal inverteria o lançamento.
    /// </summary>
    private static bool TryTakeAmount(
        IReadOnlyList<PdfWord> words, out decimal amount, out bool isCredit, out int wordCount)
    {
        amount = 0m;
        isCredit = false;
        wordCount = 0;

        if (words.Count < 2)
            return false;

        var candidate = $"{words[^2].Text} {words[^1].Text}";
        if (!AmountRegex().IsMatch(candidate))
            return false;

        if (!StatementValueParser.TryParseAmount(candidate, out amount, out _))
            return false;

        wordCount = 2;

        if (words.Count >= 3 && words[^3].Text == "+")
        {
            isCredit = true;
            wordCount = 3;
        }

        return true;
    }

    private static bool TryReadTableHeader(PdfLine line, out double beneficiaryLeft)
    {
        beneficiaryLeft = 0;

        if (!Contains(line.Text, "MOVIMENTACAO") || !Contains(line.Text, "VALOR"))
            return false;

        var beneficiary = line.Words.FirstOrDefault(w => Contains(w.Text, "BENEFICIARIO"));
        if (beneficiary is not null)
            beneficiaryLeft = beneficiary.Left;

        return true;
    }

    /// <summary>Totais por cartão, títulos de cartão e o próprio marcador de seção.</summary>
    private static bool IsNoise(string text)
    {
        var normalized = StatementTextNormalizer.Normalize(text);

        return normalized.Length == 0
            || normalized.StartsWith("TOTAL", StringComparison.Ordinal)
            || normalized.StartsWith("CARTAO", StringComparison.Ordinal)
            || normalized.Contains(ExpensesMarker, StringComparison.Ordinal);
    }

    private static bool Contains(string text, string normalizedNeedle) =>
        StatementTextNormalizer.Normalize(text).Contains(normalizedNeedle, StringComparison.Ordinal);
}
