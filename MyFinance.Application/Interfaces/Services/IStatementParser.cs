using MyFinance.Application.Dtos.StatementImport;

namespace MyFinance.Application.Interfaces.Services;

/// <summary>
/// Leitor determinístico de um formato de extrato. Cada implementação conhece um
/// layout específico e não depende de serviço externo algum — é o que garante a
/// importação com o agente de IA fora do ar.
/// </summary>
public interface IStatementParser
{
    /// <summary>Nome curto do parser, devolvido ao frontend para diagnóstico.</summary>
    string Name { get; }

    /// <summary>
    /// Ordem de tentativa: quanto menor, mais cedo o parser é consultado.
    /// Parsers específicos vêm antes dos genéricos.
    /// </summary>
    int Priority { get; }

    /// <summary>Indica se este parser reconhece o formato do arquivo.</summary>
    bool CanParse(StatementFile file);

    /// <summary>
    /// Extrai as transações do arquivo. Uma lista vazia significa que o formato
    /// parecia compatível mas não havia nada legível — o orquestrador segue para
    /// o próximo parser.
    /// </summary>
    IReadOnlyList<ParsedStatementEntry> Parse(StatementFile file);
}
