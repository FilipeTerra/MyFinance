using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MyFinance.Application.Services.StatementImport;

/// <summary>
/// Normalização das descrições de lançamento. É usada tanto na hora de gravar
/// uma regra aprendida quanto na hora de procurá-la, então precisa ser o único
/// lugar onde essa chave é construída — duas normalizações diferentes fariam o
/// aprendizado nunca ser encontrado de volta.
/// </summary>
public static partial class StatementTextNormalizer
{
    /// <summary>Sufixos de país que as maquininhas anexam à descrição.</summary>
    private static readonly HashSet<string> CountrySuffixes =
        new(StringComparer.Ordinal) { "BRA", "BR", "USA", "US", "ARG", "PRT", "ESP", "GBR" };

    /// <summary>
    /// Termos societários que não ajudam a identificar o comerciante. "COMERCIO"
    /// fica de fora de propósito: em nome de estabelecimento brasileiro ele
    /// costuma ser parte do nome, não ruído.
    /// </summary>
    private static readonly HashSet<string> NoiseTokens =
        new(StringComparer.Ordinal) { "LTDA", "ME", "EPP", "SA", "EIRELI" };

    [GeneratedRegex(@"\(PARCELA\s+\d+\s+DE\s+\d+\)|\bPARCELA\s+\d+\s*[/DE]+\s*\d+\b", RegexOptions.IgnoreCase)]
    private static partial Regex InstallmentSuffixRegex();

    [GeneratedRegex(@"[^A-Z0-9 ]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex ExtraSpacesRegex();

    /// <summary>
    /// Chave canônica de uma descrição: maiúsculas, sem acentos, sem pontuação,
    /// sem sufixo de parcela, sem código numérico longo e sem sufixo de país.
    /// Ex.: "IFD*IFOOD CLUB   Osasco   BRA" e "IFD*IFOOD CLUB" viram a mesma coisa
    /// quando o país é o único ruído; a praça é tratada no <see cref="MerchantToken"/>.
    /// </summary>
    public static string Normalize(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        var text = RemoveDiacritics(description).ToUpperInvariant();
        text = InstallmentSuffixRegex().Replace(text, " ");
        text = NonAlphanumericRegex().Replace(text, " ");
        text = ExtraSpacesRegex().Replace(text, " ").Trim();

        // Códigos de estabelecimento (ex.: "JIM COM 58154130 GIO") variam entre
        // faturas para o mesmo comerciante, então não podem entrar na chave.
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !(t.Length >= 4 && t.All(char.IsDigit)))
            .ToList();

        while (tokens.Count > 1 && CountrySuffixes.Contains(tokens[^1]))
            tokens.RemoveAt(tokens.Count - 1);

        return string.Join(' ', tokens);
    }

    /// <summary>
    /// Identificador aproximado do comerciante: os primeiros termos da descrição
    /// normalizada, o bastante para que "JIM COM LA GIO CONFE BELO HORIZON" e
    /// "JIM COM GIO BELO HORIZON" caiam no mesmo balde. Usado só para *sugerir*
    /// categoria — nunca para atribuir uma automaticamente.
    /// Devolve string vazia quando a descrição é curta demais para ser distintiva.
    /// </summary>
    public static string MerchantToken(string? description)
    {
        var normalized = Normalize(description);
        if (normalized.Length == 0)
            return string.Empty;

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var selected = new List<string>();
        var letters = 0;

        foreach (var token in tokens)
        {
            if (selected.Count > 0 && NoiseTokens.Contains(token))
                continue;

            selected.Add(token);
            letters += token.Length;

            if (letters >= 6 || selected.Count == 3)
                break;
        }

        var candidate = string.Join(' ', selected);
        return candidate.Replace(" ", string.Empty).Length >= 3 ? candidate : string.Empty;
    }

    /// <summary>Normaliza nome de categoria para Title Case. Ex.: "DROGARIA" → "Drogaria".</summary>
    public static string ToTitleCase(string name)
    {
        var trimmed = name.Trim();
        return trimmed.Length == 0
            ? trimmed
            : CultureInfo.GetCultureInfo("pt-BR").TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
    }

    private static string RemoveDiacritics(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
