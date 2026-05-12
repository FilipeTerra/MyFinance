using MyFinance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Application.Dtos;

namespace MyFinance.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    /// <summary>
    /// Busca uma transa��o pelo seu Id, verificando indiretamente o usu�rio atrav�s da conta.
    /// Inclui a entidade Account relacionada.
    /// </summary>
    Task<Transaction?> GetByIdAsync(Guid id, Guid userId);

    /// <summary>
    /// Busca todas as transa��es de uma conta espec�fica pertencente ao usu�rio.
    /// </summary>
    Task<IEnumerable<Transaction>> GetAllByAccountIdAsync(Guid accountId, Guid userId);

    /// <summary>
    /// Adiciona uma nova transa��o ao contexto do EF.
    /// </summary>
    Task AddAsync(Transaction transaction);

    /// <summary>
    /// Marca uma transa��o como modificada no contexto do EF.
    /// </summary>
    void Update(Transaction transaction);

    /// <summary>
    /// Marca uma transa��o como removida no contexto do EF.
    /// </summary>
    void Delete(Transaction transaction);

    /// <summary>
    /// Verifica se uma conta possui alguma transa��o associada.
    /// </summary>
    Task<bool> HasTransactionsAsync(Guid accountId);

    /// <summary>
    /// Salva todas as mudan�as pendentes (Add, Update, Delete) no banco de dados.
    /// </summary>
    Task<bool> SaveChangesAsync();

    /// <summary>
    /// Inicia uma transa��o de banco de dados para garantir atomicidade.
    /// </summary>
    Task<ITransactionDbTransaction> BeginTransactionAsync();

    /// <summary>
    /// Busca transa��es com base em um conjunto de filtros dinâmicos.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByFilterAsync(Guid userId, TransactionSearchRequestDto filters);

    Task AddRangeAsync(IEnumerable<Transaction> transactions);
}