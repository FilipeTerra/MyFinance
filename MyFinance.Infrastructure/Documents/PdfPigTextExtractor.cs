using Microsoft.Extensions.Logging;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Services;
using UglyToad.PdfPig;

namespace MyFinance.Infrastructure.Documents;

/// <summary>
/// Extração de texto de PDF com a PdfPig — biblioteca 100% gerenciada, sem
/// dependência nativa, o que mantém o deploy da API igual ao que já é hoje.
///
/// Além do texto, preserva a posição horizontal de cada palavra: é o que permite
/// aos parsers separarem as colunas de uma tabela pelo cabeçalho, em vez de
/// adivinhar onde uma coluna acaba.
/// </summary>
public class PdfPigTextExtractor : IPdfTextExtractor
{
    /// <summary>
    /// Palavras cujas linhas de base diferem menos que isto pertencem à mesma
    /// linha visual. Valor em pontos de PDF (1/72"), abaixo da altura de uma
    /// linha de texto comum e acima do ruído de arredondamento da fonte.
    /// </summary>
    private const double SameLineTolerance = 3.0;

    private readonly ILogger<PdfPigTextExtractor> _logger;

    public PdfPigTextExtractor(ILogger<PdfPigTextExtractor> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<PdfLine> ExtractLines(byte[] pdfContent)
    {
        var lines = new List<PdfLine>();

        using var document = PdfDocument.Open(pdfContent);

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords()
                .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                .ToList();

            if (words.Count == 0)
                continue;

            // No PDF o eixo Y cresce para cima, então a ordem de leitura é do
            // maior para o menor.
            foreach (var group in GroupByBaseline(words))
            {
                var ordered = group
                    .OrderBy(w => w.BoundingBox.Left)
                    .Select(w => new PdfWord(w.Text, w.BoundingBox.Left, w.BoundingBox.Right))
                    .ToList();

                lines.Add(new PdfLine(page.Number, ordered));
            }
        }

        if (lines.Count == 0)
            _logger.LogWarning("PDF sem camada de texto: nenhuma linha extraída (documento digitalizado?).");

        return lines;
    }

    private static IEnumerable<List<UglyToad.PdfPig.Content.Word>> GroupByBaseline(
        List<UglyToad.PdfPig.Content.Word> words)
    {
        var current = new List<UglyToad.PdfPig.Content.Word>();
        var currentBaseline = double.NaN;

        foreach (var word in words.OrderByDescending(w => w.BoundingBox.Bottom))
        {
            var baseline = word.BoundingBox.Bottom;

            if (current.Count > 0 && Math.Abs(baseline - currentBaseline) > SameLineTolerance)
            {
                yield return current;
                current = new List<UglyToad.PdfPig.Content.Word>();
            }

            if (current.Count == 0)
                currentBaseline = baseline;

            current.Add(word);
        }

        if (current.Count > 0)
            yield return current;
    }
}
