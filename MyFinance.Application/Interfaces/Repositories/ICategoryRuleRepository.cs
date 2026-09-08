using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Interfaces.Repositories;

public interface ICategoryRuleRepository
{
    /// <summary>Todas as regras aprendidas de um usuário.</summary>
    Task<IEnumerable<CategoryRule>> GetAllByUserIdAsync(Guid userId);

    /// <summary>
    /// Cria ou repõe as regras informadas no contexto do EF, sem persistir:
    /// quem chama decide quando salvar, o que permite gravar as regras dentro da
    /// mesma transação de banco do lote de transações.
    /// </summary>
    Task UpsertRangeAsync(Guid userId, IReadOnlyCollection<CategoryRuleDraft> drafts);

    /// <summary>Salva as mudanças pendentes no banco de dados.</summary>
    Task<bool> SaveChangesAsync();
}
