using System.Text;

namespace MyFinance.Application.Dtos.StatementImport;

/// <summary>
/// Arquivo de extrato enviado pelo usuário, já materializado em memória.
/// Os parsers precisam reler o conteúdo mais de uma vez (identificação do
/// formato e, depois, o parse em si), o que o stream de upload — que é
/// consumido uma única vez — não permitiria.
/// </summary>
public sealed class StatementFile
{
    private string? _text;

    public StatementFile(string fileName, string? contentType, byte[] content)
    {
        FileName = fileName;
        ContentType = contentType ?? string.Empty;
        Content = content;
    }

    public string FileName { get; }
    public string ContentType { get; }
    public byte[] Content { get; }

    public string Extension => Path.GetExtension(FileName).ToLowerInvariant();

    public bool IsPdf =>
        Extension == ".pdf" || ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Conteúdo como texto. Tenta UTF-8 estrito (cobrindo o BOM) e, se o arquivo
    /// não for UTF-8 válido, cai para ISO-8859-1 — encoding comum em exportações
    /// de bancos brasileiros, e que nunca falha na decodificação.
    /// </summary>
    public string ReadAsText()
    {
        if (_text is not null)
            return _text;

        try
        {
            var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            _text = strictUtf8.GetString(StripBom(Content));
        }
        catch (DecoderFallbackException)
        {
            _text = Encoding.Latin1.GetString(Content);
        }

        return _text;
    }

    private static byte[] StripBom(byte[] content)
    {
        if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
            return content[3..];

        return content;
    }
}
