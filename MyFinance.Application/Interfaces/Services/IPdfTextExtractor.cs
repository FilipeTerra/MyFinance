using MyFinance.Application.Dtos.StatementImport;

namespace MyFinance.Application.Interfaces.Services;

/// <summary>
/// Extrai o texto de um PDF preservando a posição horizontal das palavras.
/// A implementação concreta vive na camada de infraestrutura (biblioteca de PDF).
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>
    /// Devolve as linhas do documento, na ordem de leitura. Lança se o PDF
    /// estiver criptografado ou corrompido; devolve vazio se não houver camada
    /// de texto (documento digitalizado).
    /// </summary>
    IReadOnlyList<PdfLine> ExtractLines(byte[] pdfContent);
}
