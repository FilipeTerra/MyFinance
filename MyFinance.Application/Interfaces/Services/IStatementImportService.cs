using MyFinance.Application.Dtos.StatementImport;

namespace MyFinance.Application.Interfaces.Services;

/// <summary>
/// Orquestra a importação de um extrato: escolhe o parser, resolve as categorias
/// de forma determinística e só então — se ainda fizer sentido — recorre à IA.
/// </summary>
public interface IStatementImportService
{
    Task<StatementImportResultDto> ImportAsync(StatementFile file, Guid accountId, Guid userId);
}
