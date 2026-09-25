using System.Text.RegularExpressions;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Reconhece a marcação de parcela que os bancos anexam ao lançamento da fatura.
/// É o único lugar que sabe esses formatos: o mesmo texto precisa ser lido para
/// extrair "6 de 9" e apagado para montar a chave de categoria, e duas regex
/// diferentes acabariam divergindo.
/// </summary>
/// <remarks>
/// Formatos aceitos: "(Parcela 06 de 09)" e "Parcela 2/3" (descrição do PDF e coluna
/// "Tipo" do CSV do Inter), "PARC 02/12", as versões entre parênteses sem a palavra
/// ("(2/12)") e o campo que contém só a fração ("2/3", quando vem numa coluna própria).
///
/// Um "02/12" solto no meio da descrição é ignorado de propósito — não há como
/// distingui-lo de uma data, e ler errado inventaria dívida que não existe.
/// </remarks>
public static partial class InstallmentParser
{
    /// <summary>Teto de sanidade — acima disso é quase certo que o número lido não é parcela.</summary>
    private const int MaxInstallments = 99;

    /// <summary>"Parcela 06 de 09", "Parcela 2/3", "PARC 02/12".</summary>
    [GeneratedRegex(@"\bPARC(?:ELAS?)?\b\.?\s*(\d{1,2})\s*(?:DE|/)\s*(\d{1,2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex KeywordRegex();

    /// <summary>"(6 de 9)", "(2/12)" — os parênteses bastam para não confundir com data.</summary>
    [GeneratedRegex(@"\((\d{1,2})\s*(?:DE|/)\s*(\d{1,2})\)", RegexOptions.IgnoreCase)]
    private static partial Regex ParenthesizedRegex();

    /// <summary>Campo que contém só a fração, como uma coluna "Tipo" preenchida com "2/3".</summary>
    [GeneratedRegex(@"^\s*(\d{1,2})\s*(?:DE|/|X)\s*(\d{1,2})\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex WholeFieldRegex();

    /// <summary>
    /// Mesma marcação da <see cref="KeywordRegex"/>, com os parênteses em volta quando
    /// existirem, para sumir por inteiro da descrição normalizada.
    /// </summary>
    [GeneratedRegex(@"\(?\s*\bPARC(?:ELAS?)?\b\.?\s*\d{1,2}\s*(?:DE|/)\s*\d{1,2}\s*\)?|\(\d{1,2}\s*(?:DE|/)\s*\d{1,2}\)", RegexOptions.IgnoreCase)]
    private static partial Regex SuffixRegex();

    /// <summary>
    /// Extrai o número e o total de parcelas de um texto de extrato.
    /// </summary>
    /// <param name="text">Descrição do lançamento ou o conteúdo de uma coluna dedicada.</param>
    /// <returns>
    /// Falso quando o texto não traz parcela ou quando os números lidos não formam um
    /// parcelamento válido (parcela maior que o total, total zerado ou absurdo).
    /// </returns>
    public static bool TryParse(string? text, out int number, out int total)
    {
        number = 0;
        total = 0;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var match = KeywordRegex().Match(text);
        if (!match.Success)
            match = ParenthesizedRegex().Match(text);
        if (!match.Success)
            match = WholeFieldRegex().Match(text);
        if (!match.Success)
            return false;

        if (!int.TryParse(match.Groups[1].Value, out number) || !int.TryParse(match.Groups[2].Value, out total))
            return false;

        if (total < 1 || total > MaxInstallments || number < 1 || number > total)
        {
            number = 0;
            total = 0;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Remove a marcação de parcela do texto. Usado para montar a chave canônica de
    /// descrição, em que todas as parcelas de uma compra precisam colapsar na mesma coisa.
    /// </summary>
    public static string RemoveFrom(string text) => SuffixRegex().Replace(text, " ");
}
