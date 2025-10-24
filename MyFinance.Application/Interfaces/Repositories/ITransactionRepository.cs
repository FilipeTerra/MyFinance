using MyFinance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Application.Dtos;

namespace MyFinance.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    /// <summary>
    /// Busca uma transação pelo seu Id, verificando indiretamente o usuário através da conta.
    /// Inclui a entidade Account relacionada.
    /// </summary>
    Task<Transaction?> GetByIdAsync(Guid id, Guid userId);

    /// <summary>
    /// Busca todas as transações de uma conta específica pertencente ao usuário.
    /// </summary>
    Task<IEnumerable<Transaction>> GetAllByAccountIdAsync(Guid accountId, Guid userId);

    /// <summary>
    /// Adiciona uma nova transação ao contexto do EF.
    /// </summary>
    Task AddAsync(Transaction transaction);

    /// <summary>
    /// Marca uma transação como modificada no contexto do EF.
    /// </summary>
    void Update(Transaction transaction);

    /// <summary>
    /// Marca uma transação como removida no contexto do EF.
    /// </summary>
    void Delete(Transaction transaction);

    /// <summary>
    /// Verifica se uma conta possui alguma transação associada.
    /// </summary>
    Task<bool> HasTransactionsAsync(Guid accountId);

    /// <summary>
    /// Salva todas as mudanças pendentes (Add, Update, Delete) no banco de dados.
    /// </summary>
    Task<bool> SaveChangesAsync();

    /// <summary>
    /// Busca transações com base em um conjunto de filtros dinâmicos.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByFilterAsync(Guid userId, TransactionSearchRequestDto filters);
}