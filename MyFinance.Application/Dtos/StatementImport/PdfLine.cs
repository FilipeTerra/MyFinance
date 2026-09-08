namespace MyFinance.Application.Dtos.StatementImport;

/// <summary>Palavra extraída de um PDF, com sua posição horizontal na página.</summary>
/// <param name="Text">Texto da palavra.</param>
/// <param name="Left">Coordenada X da borda esquerda.</param>
/// <param name="Right">Coordenada X da borda direita.</param>
public sealed record PdfWord(string Text, double Left, double Right);

/// <summary>
/// Linha de texto de um PDF. Além do texto concatenado, mantém a posição de cada
/// palavra — é o que permite fatiar uma tabela pelas colunas do cabeçalho em vez
/// de adivinhar onde uma coluna termina e a outra começa.
/// </summary>
public sealed record PdfLine(int PageNumber, IReadOnlyList<PdfWord> Words)
{
    public string Text => string.Join(" ", Words.Select(w => w.Text));
}
