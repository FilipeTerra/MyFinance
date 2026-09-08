namespace MyFinance.Application.Dtos.StatementImport;

/// <summary>
/// Resultado da importação de um extrato. Além das transações, informa como o
/// arquivo foi lido e se a IA participou — o frontend usa isso para não prometer
/// uma classificação inteligente que não aconteceu.
/// </summary>
public class StatementImportResultDto
{
    /// <summary>Falso quando nenhum caminho conseguiu interpretar o arquivo.</summary>
    public bool Success { get; set; } = true;

    /// <summary>Mensagem para o usuário quando <see cref="Success"/> é falso.</summary>
    public string? Message { get; set; }

    public List<AiTransactionResponseDto> Transactions { get; set; } = new();

    /// <summary>
    /// Nome do parser que reconheceu o arquivo (ou "IA" quando veio do agente).
    /// Só é preenchido quando exatamente um arquivo teve sucesso — com vários
    /// arquivos o detalhe por arquivo vive em <see cref="Files"/>.
    /// </summary>
    public string? ParserUsed { get; set; }

    /// <summary>Resultado de cada arquivo do lote, sucesso ou falha.</summary>
    public List<FileImportSummaryDto> Files { get; set; } = new();

    /// <summary>Verdadeiro se o agente de IA foi efetivamente usado nesta importação.</summary>
    public bool AiUsed { get; set; }

    /// <summary>
    /// Verdadeiro se a IA foi consultada e não respondeu. A importação segue
    /// normalmente — apenas sem o enriquecimento que ela traria.
    /// </summary>
    public bool AiUnavailable { get; set; }

    /// <summary>Quantas transações do arquivo já existem na conta.</summary>
    public int DuplicateCount { get; set; }

    public List<string> Warnings { get; set; } = new();
}
