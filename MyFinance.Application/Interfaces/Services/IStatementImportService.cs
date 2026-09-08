using MyFinance.Application.Dtos.StatementImport;

namespace MyFinance.Application.Interfaces.Services;

/// <summary>
/// Orquestra a importação de um extrato: escolhe o parser, resolve as categorias
/// de forma determinística e só então — se ainda fizer sentido — recorre à IA.
/// </summary>
public interface IStatementImportService
{
    Task<StatementImportResultDto> ImportAsync(StatementFile file, Guid accountId, Guid userId);

    /// <summary>
    /// Importa um lote de arquivos de uma vez. A leitura de cada arquivo roda em
    /// paralelo (não toca banco); a resolução de categoria, a checagem de
    /// duplicata e a consulta à IA rodam uma única vez para o lote inteiro, não
    /// uma vez por arquivo.
    /// </summary>
    Task<StatementImportResultDto> ImportAsync(
        IReadOnlyList<StatementFile> files, Guid accountId, Guid userId, CancellationToken cancellationToken = default);
}
