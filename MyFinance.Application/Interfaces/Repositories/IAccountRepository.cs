using MyFinance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Repositories;

public interface IAccountRepository
{
    /// <summary>
    /// Busca uma conta pelo Id, garantindo que ela pertença ao usuário (segurança).
    /// </summary>
    Task<Account?> GetByIdAsync(Guid id, Guid userId);

    /// <summary>
    /// Busca todas as contas de um usuário específico.
    /// </summary>
    Task<IEnumerable<Account>> GetAllByUserIdAsync(Guid userId);

    /// <summary>
    /// Adiciona uma nova conta ao contexto do EF.
    /// </summary>
    Task AddAsync(Account account);

    /// <summary>
    /// Marca uma conta como modificada no contexto do EF.
    /// </summary>
    void Update(Account account);

    /// <summary>
    /// Marca uma conta como removida no contexto do EF.
    /// </summary>
    void Delete(Account account);

    /// <summary>
    /// Salva todas as mudanças (Add, Update, Delete) no banco de dados.
    /// </summary>
    Task<bool> SaveChangesAsync();
}