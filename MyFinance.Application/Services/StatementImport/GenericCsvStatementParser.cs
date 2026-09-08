using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Último recurso determinístico: qualquer arquivo delimitado em que se consiga
/// identificar, pelo cabeçalho, as colunas de data, descrição e valor. Cobre
/// exportações de outros bancos sem precisar de um parser dedicado para cada um.
/// </summary>
public sealed class GenericCsvStatementParser : IStatementParser
{
    private static readonly string[] DateAliases =
        { "DATA", "DATE", "DT", "DATA LANCAMENTO", "DATA DA COMPRA", "DATA MOVIMENTO" };

    private static readonly string[] DescriptionAliases =
        { "DESCRICAO", "LANCAMENTO", "HISTORICO", "MOVIMENTACAO", "DESCRIPTION", "MEMO", "ESTABELECIMENTO", "TITULO" };

    private static readonly string[] AmountAliases =
        { "VALOR", "AMOUNT", "VALUE", "VALOR RS", "VALOR BRL", "MONTANTE", "QUANTIA" };

    private static readonly string[] CategoryAliases =
        { "CATEGORIA", "CATEGORY" };

    private static readonly string[] TypeAliases =
        { "TIPO", "TYPE", "NATUREZA", "OPERACAO" };

    private static readonly HashSet<string> GenericCategories =
        new(StringComparer.OrdinalIgnoreCase) { "OUTROS", "OUTRAS", "DIVERSOS" };

    public string Name => "CSV genérico";

    /// <summary>Depois dos parsers específicos: só entra quando ninguém reconheceu o layout.</summary>
    public int Priority => 90;

    public bool CanParse(StatementFile file)
    {
        if (file.IsPdf)
            return false;

        return TryLocateColumns(file, out _, out _);
    }

    public IReadOnlyList<ParsedStatementEntry> Parse(StatementFile file)
    {
        if (!TryLocateColumns(file, out var rows, out var layout))
            return Array.Empty<ParsedStatementEntry>();

        var entries = new List<ParsedStatementEntry>();

        foreach (var row in rows.Skip(1))
        {
            var rawDate = At(row, layout.DateIndex);
            var description = At(row, layout.DescriptionIndex);
            var rawAmount = At(row, layout.AmountIndex);

            if (string.IsNullOrWhiteSpace(rawDate)
                || string.IsNullOrWhiteSpace(description)
                || string.IsNullOrWhiteSpace(rawAmount))
                continue;

            if (!StatementValueParser.TryParseDate(rawDate, out var date))
                continue;

            if (!StatementValueParser.TryParseAmount(rawAmount, out var amount, out var isNegative))
                continue;

            // Convenção de extrato bancário, oposta à da fatura de cartão: aqui o
            // sinal negativo é gasto. O campo "Tipo", quando existe, vale mais que
            // o sinal — há exportações que trazem todos os valores positivos e
            // deixam a natureza só nessa coluna.
            var tipo = At(row, layout.TypeIndex);
            var isCredit = StatementValueParser.IsCreditType(tipo)
                || (!StatementValueParser.IsDebitType(tipo) && !isNegative);

            var fileCategory = At(row, layout.CategoryIndex);

            entries.Add(new ParsedStatementEntry(
                date,
                description.Trim(),
                isCredit ? amount : -amount,
                string.IsNullOrWhiteSpace(fileCategory) || GenericCategories.Contains(fileCategory.Trim())
                    ? null
                    : StatementTextNormalizer.ToTitleCase(fileCategory)));
        }

        return entries;
    }

    private static string? At(string[] row, int index) =>
        index >= 0 && index < row.Length ? row[index] : null;

    private static bool TryLocateColumns(StatementFile file, out List<string[]> rows, out ColumnLayout layout)
    {
        rows = new List<string[]>();
        layout = ColumnLayout.Empty;

        var content = file.ReadAsText();
        if (string.IsNullOrWhiteSpace(content))
            return false;

        rows = DelimitedTextReader.ReadRows(content, DelimitedTextReader.SniffDelimiter(content));
        if (rows.Count < 2)
            return false;

        var header = rows[0].Select(StatementTextNormalizer.Normalize).ToArray();

        layout = new ColumnLayout(
            IndexOfAlias(header, DateAliases),
            IndexOfAlias(header, DescriptionAliases),
            IndexOfAlias(header, AmountAliases),
            IndexOfAlias(header, CategoryAliases),
            IndexOfAlias(header, TypeAliases));

        return layout.IsUsable;
    }

    /// <summary>
    /// Casa o cabeçalho com os apelidos conhecidos: primeiro por igualdade, e só
    /// depois por conter o apelido — evita que "DATA DE VENCIMENTO" roube a vez de
    /// uma coluna "DATA" existente.
    /// </summary>
    private static int IndexOfAlias(string[] header, string[] aliases)
    {
        for (var i = 0; i < header.Length; i++)
        {
            if (aliases.Any(alias => string.Equals(header[i], alias, StringComparison.Ordinal)))
                return i;
        }

        for (var i = 0; i < header.Length; i++)
        {
            if (header[i].Length > 0 && aliases.Any(alias => header[i].Contains(alias, StringComparison.Ordinal)))
                return i;
        }

        return -1;
    }

    private readonly record struct ColumnLayout(
        int DateIndex,
        int DescriptionIndex,
        int AmountIndex,
        int CategoryIndex,
        int TypeIndex)
    {
        public static ColumnLayout Empty => new(-1, -1, -1, -1, -1);

        public bool IsUsable => DateIndex >= 0 && DescriptionIndex >= 0 && AmountIndex >= 0;
    }
}
