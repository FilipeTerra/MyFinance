namespace MyFinance.Application.Dtos.StatementImport;

/// <summary>
/// Resultado da leitura de um único arquivo dentro de um lote de importação.
/// Existe para o usuário saber, quando envia vários arquivos de uma vez, qual
/// deles falhou e por quê — sem isso um lote parcialmente bem-sucedido só
/// apareceria como "algumas transações a menos", sem explicação.
/// </summary>
public class FileImportSummaryDto
{
    public string FileName { get; set; } = string.Empty;

    public bool Success { get; set; }

    /// <summary>Motivo da falha. Preenchido só quando <see cref="Success"/> é falso.</summary>
    public string? Message { get; set; }

    /// <summary>Nome do parser que leu o arquivo (ou "IA"), quando teve sucesso.</summary>
    public string? ParserUsed { get; set; }

    public bool AiUsed { get; set; }

    public int TransactionCount { get; set; }
}
