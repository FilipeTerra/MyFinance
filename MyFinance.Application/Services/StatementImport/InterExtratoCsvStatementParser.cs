using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Parser do CSV de EXTRATO DE CONTA CORRENTE do Banco Inter, cujo cabeçalho é
/// "Data Lançamento","Descrição","Valor","Saldo". Diferente da fatura de
/// cartão (<see cref="InterCsvStatementParser"/>), o arquivo traz linhas de
/// metadado (título, número da conta, período, saldo) antes do cabeçalho real,
/// então a coluna precisa ser localizada em vez de assumida na primeira linha.
///
/// A convenção de sinal aqui é a do extrato bancário, não a da fatura: não há
/// coluna "Tipo", então o sinal do próprio valor já diz se é despesa (negativo)
/// ou receita (positivo).
/// </summary>
public sealed class InterExtratoCsvStatementParser : IStatementParser
{
    private const string ColumnDate = "DATA LANCAMENTO";
    private const string ColumnDescription = "DESCRICAO";
    private const string ColumnAmount = "VALOR";
    private const string ColumnBalance = "SALDO";

    private static readonly string[] RequiredColumns =
        { ColumnDate, ColumnDescription, ColumnAmount, ColumnBalance };

    /// <summary>
    /// Quantas linhas do início do arquivo são varridas em busca do cabeçalho.
    /// O preâmbulo real do Inter tem 5 linhas; a folga tolera pequenas variações
    /// do banco sem escanear o arquivo inteiro à toa.
    /// </summary>
    private const int MaxHeaderSearchLines = 20;

    public string Name => "Inter Extrato CSV";

    public int Priority => 15;

    public bool CanParse(StatementFile file)
    {
        if (file.IsPdf)
            return false;

        return TryLocateHeader(file, out _, out _);
    }

    public IReadOnlyList<ParsedStatementEntry> Parse(StatementFile file)
    {
        if (!TryLocateHeader(file, out var rows, out var columns))
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

            entries.Add(new ParsedStatementEntry(date, description.Trim(), isNegative ? -amount : amount));
        }

        return entries;
    }

    private static string? Field(string[] row, IReadOnlyDictionary<string, int> columns, string column)
    {
        if (!columns.TryGetValue(column, out var index) || index >= row.Length)
            return null;

        return row[index];
    }

    /// <summary>
    /// Procura, entre as primeiras <see cref="MaxHeaderSearchLines"/> linhas do
    /// arquivo, aquela que contém as colunas obrigatórias — pulando o preâmbulo
    /// de metadados. O delimitador é descoberto a partir dessa linha específica,
    /// não da primeira linha do arquivo (que não tem separador algum).
    /// </summary>
    private static bool TryLocateHeader(
        StatementFile file,
        out List<string[]> rows,
        out Dictionary<string, int> columns)
    {
        rows = new List<string[]>();
        columns = new Dictionary<string, int>(StringComparer.Ordinal);

        var content = file.ReadAsText();
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var lines = ReadLines(content);

        for (var i = 0; i < lines.Count && i < MaxHeaderSearchLines; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var delimiter = DelimitedTextReader.SniffDelimiter(line);
            var headerFields = DelimitedTextReader.ReadRows(line, delimiter).FirstOrDefault();
            if (headerFields is null)
                continue;

            var candidateColumns = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var c = 0; c < headerFields.Length; c++)
            {
                var key = StatementTextNormalizer.Normalize(headerFields[c]);
                if (key.Length > 0 && !candidateColumns.ContainsKey(key))
                    candidateColumns[key] = c;
            }

            if (!RequiredColumns.All(candidateColumns.ContainsKey))
                continue;

            var remaining = string.Join("\n", lines.Skip(i));
            var parsedRows = DelimitedTextReader.ReadRows(remaining, delimiter);
            if (parsedRows.Count < 2)
                return false;

            rows = parsedRows;
            columns = candidateColumns;
            return true;
        }

        return false;
    }

    private static List<string> ReadLines(string content)
    {
        var lines = new List<string>();
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) is not null)
            lines.Add(line);

        return lines;
    }
}
