using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Parser do CSV de FATURA DE CARTÃO do Banco Inter, cujo cabeçalho é
/// "Data","Lançamento","Categoria","Tipo","Valor".
///
/// A regra de sinal aqui é a da fatura, não a do extrato bancário: como toda
/// linha é uma despesa do cartão, um valor negativo significa dinheiro voltando
/// (pagamento da fatura, estorno). O extrato de conta, com outro cabeçalho, cai
/// no GenericCsvStatementParser, que usa a convenção inversa.
///
/// É a porta determinística do que o agente de IA já fazia sem LLM
/// (ProcessFileSemanticUseCase._try_parse_csv), agora do lado da API.
/// </summary>
public sealed class InterCsvStatementParser : IStatementParser
{
    private const string ColumnDate = "DATA";
    private const string ColumnDescription = "LANCAMENTO";
    private const string ColumnAmount = "VALOR";
    private const string ColumnCategory = "CATEGORIA";
    private const string ColumnType = "TIPO";

    private static readonly string[] RequiredColumns = { ColumnDate, ColumnDescription, ColumnAmount };

    /// <summary>Categorias que o banco preenche por falta de opção melhor.</summary>
    private static readonly HashSet<string> GenericCategories =
        new(StringComparer.OrdinalIgnoreCase) { "OUTROS", "OUTRAS", "DIVERSOS" };

    public string Name => "Inter CSV";

    public int Priority => 10;

    public bool CanParse(StatementFile file)
    {
        if (file.IsPdf)
            return false;

        return TryReadHeader(file, out _, out _);
    }

    public IReadOnlyList<ParsedStatementEntry> Parse(StatementFile file)
    {
        if (!TryReadHeader(file, out var rows, out var columns))
            return Array.Empty<ParsedStatementEntry>();

        var entries = new List<ParsedStatementEntry>();

        foreach (var row in rows.Skip(1))
        {
            var rawDate = Field(row, columns, ColumnDate);
            var description = Field(row, columns, ColumnDescription);
            var rawAmount = Field(row, columns, ColumnAmount);

            if (string.IsNullOrWhiteSpace(rawDate)
                || string.IsNullOrWhiteSpace(description)
                || string.IsNullOrWhiteSpace(rawAmount))
                continue;

            if (!StatementValueParser.TryParseDate(rawDate, out var date))
                continue;

            if (!StatementValueParser.TryParseAmount(rawAmount, out var amount, out var isNegative))
                continue;

            // Numa fatura de cartão, valor negativo é dinheiro voltando (pagamento
            // da fatura, estorno). O sinal tem prioridade sobre o campo "Tipo".
            var isCredit = isNegative || StatementValueParser.IsCreditType(Field(row, columns, ColumnType));

            entries.Add(new ParsedStatementEntry(
                date,
                description.Trim(),
                isCredit ? amount : -amount,
                ResolveFileCategory(Field(row, columns, ColumnCategory))));
        }

        return entries;
    }

    private static string? ResolveFileCategory(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || GenericCategories.Contains(raw.Trim()))
            return null;

        return StatementTextNormalizer.ToTitleCase(raw);
    }

    private static string? Field(string[] row, IReadOnlyDictionary<string, int> columns, string column)
    {
        if (!columns.TryGetValue(column, out var index) || index >= row.Length)
            return null;

        return row[index];
    }

    private static bool TryReadHeader(
        StatementFile file,
        out List<string[]> rows,
        out Dictionary<string, int> columns)
    {
        rows = new List<string[]>();
        columns = new Dictionary<string, int>(StringComparer.Ordinal);

        var content = file.ReadAsText();
        if (string.IsNullOrWhiteSpace(content))
            return false;

        rows = DelimitedTextReader.ReadRows(content, DelimitedTextReader.SniffDelimiter(content));
        if (rows.Count < 2)
            return false;

        var header = rows[0];
        for (var i = 0; i < header.Length; i++)
        {
            var key = StatementTextNormalizer.Normalize(header[i]);
            if (key.Length > 0 && !columns.ContainsKey(key))
                columns[key] = i;
        }

        return RequiredColumns.All(columns.ContainsKey);
    }
}
