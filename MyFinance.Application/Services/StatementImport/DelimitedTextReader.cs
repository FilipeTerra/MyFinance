using System.Text;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Leitor de arquivos delimitados (CSV/TSV) no formato RFC 4180: respeita aspas,
/// delimitadores dentro de aspas, aspas escapadas ("") e quebras de linha dentro
/// de campo. Pequeno o bastante para não valer uma dependência externa.
/// </summary>
public static class DelimitedTextReader
{
    private static readonly char[] CandidateDelimiters = { ',', ';', '\t', '|' };

    /// <summary>
    /// Descobre o delimitador olhando o cabeçalho: vence o caractere que produz
    /// mais colunas fora de aspas. Empate ou nenhum candidato → vírgula.
    /// </summary>
    public static char SniffDelimiter(string content)
    {
        var firstLine = FirstNonEmptyLine(content);
        if (firstLine is null)
            return ',';

        var best = ',';
        var bestCount = 0;

        foreach (var candidate in CandidateDelimiters)
        {
            var count = CountOutsideQuotes(firstLine, candidate);
            if (count > bestCount)
            {
                best = candidate;
                bestCount = count;
            }
        }

        return best;
    }

    /// <summary>Lê todas as linhas do conteúdo já como campos separados.</summary>
    public static List<string[]> ReadRows(string content, char delimiter)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Aspas duplas seguidas representam uma aspa literal no campo.
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;

                case var _ when c == delimiter:
                    fields.Add(field.ToString().Trim());
                    field.Clear();
                    break;

                case '\r':
                    break;

                case '\n':
                    fields.Add(field.ToString().Trim());
                    field.Clear();
                    AppendRow(rows, fields);
                    fields.Clear();
                    break;

                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString().Trim());
            AppendRow(rows, fields);
        }

        return rows;
    }

    private static void AppendRow(List<string[]> rows, List<string> fields)
    {
        if (fields.Any(f => f.Length > 0))
            rows.Add(fields.ToArray());
    }

    private static string? FirstNonEmptyLine(string content)
    {
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (!string.IsNullOrWhiteSpace(line))
                return line;
        }

        return null;
    }

    private static int CountOutsideQuotes(string line, char delimiter)
    {
        var count = 0;
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
                inQuotes = !inQuotes;
            else if (!inQuotes && c == delimiter)
                count++;
        }

        return count;
    }
}
