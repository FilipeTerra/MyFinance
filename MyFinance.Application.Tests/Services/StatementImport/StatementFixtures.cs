using System.Text;
using MyFinance.Application.Dtos.StatementImport;

namespace MyFinance.Application.Tests.Services.StatementImport;

/// <summary>Arquivos e linhas de extrato usados pelos testes de importação.</summary>
internal static class StatementFixtures
{
    public static StatementFile InterCsv() =>
        FromFile("fatura-inter.csv", "text/csv");

    public static StatementFile FromText(string content, string fileName = "extrato.csv") =>
        new(fileName, "text/csv", Encoding.UTF8.GetBytes(content));

    public static StatementFile FromFile(string fixtureName, string contentType)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);
        return new StatementFile(fixtureName, contentType, File.ReadAllBytes(path));
    }

    public static StatementFile Pdf(string fileName = "fatura.pdf") =>
        new(fileName, "application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 });

    /// <summary>
    /// Linhas equivalentes às da fatura do Inter em PDF, já na forma que o
    /// extrator entrega. Os dados do titular e os números de cartão são fictícios.
    /// </summary>
    public static IReadOnlyList<PdfLine> InterPdfLines()
    {
        return new[]
        {
            // Páginas de resumo e simulação de parcelamento: vêm antes do marcador
            // e não podem virar transação, apesar de conterem valores em reais.
            Line(1, ("Fatura", 40), ("de", 80), ("setembro", 100), ("R$", 480), ("4.113,59", 500)),
            Line(1, ("Parcelamento", 40), ("em", 120), ("3x", 140), ("R$", 480), ("1.540,51", 500)),

            Line(2, ("Despesas", 40), ("da", 90), ("fatura", 110)),
            Line(2, ("CARTÃO", 40), ("5364****1111", 90)),
            Line(2, ("Data", 40), ("Movimentação", 110), ("Beneficiário", 390), ("Valor", 540)),
            Line(2, ("03", 40), ("de", 60), ("ago.", 80), ("2026", 105),
                    ("PAGAMENTO", 110), ("ON", 190), ("LINE", 215),
                    ("-", 390), ("+", 480), ("R$", 495), ("4.657,32", 520)),
            Line(2, ("Total", 40), ("CARTÃO", 70), ("5364****1111", 120), ("R$", 500), ("0,00", 520)),

            Line(3, ("Despesas", 40), ("da", 90), ("fatura", 110)),
            Line(3, ("CARTÃO", 40), ("5364****2222", 90)),
            Line(3, ("Data", 40), ("Movimentação", 110), ("Beneficiário", 390), ("Valor", 540)),
            Line(3, ("16", 40), ("de", 60), ("mar.", 80), ("2026", 105),
                    ("CP", 110), ("PARC", 130), ("DUO", 170), ("GOURMET", 200),
                    ("(Parcela", 260), ("06", 310), ("de", 330), ("09)", 350),
                    ("-", 390), ("R$", 500), ("59,78", 525)),
            Line(3, ("04", 40), ("de", 60), ("ago.", 80), ("2026", 105),
                    ("DL*UberRides", 110), ("-", 390), ("R$", 500), ("37,95", 525)),
            Line(3, ("Total", 40), ("CARTÃO", 70), ("5364****2222", 120), ("R$", 500), ("97,73", 525))
        };
    }

    private static PdfLine Line(int page, params (string Text, double Left)[] words)
    {
        var pdfWords = words
            .Select(w => new PdfWord(w.Text, w.Left, w.Left + (w.Text.Length * 6.0)))
            .ToList();

        return new PdfLine(page, pdfWords);
    }
}
