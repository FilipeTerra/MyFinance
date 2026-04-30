using MyFinance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Repositories;

public interface ICategoryRepository
{
    /// <summary>
    /// Busca uma categoria pelo Id, garantindo que ela perten�a ao usu�rio.
    /// </summary>
    Task<Category?> GetByIdAsync(Guid id, Guid userId);

    /// <summary>
    /// Busca todas as categorias de um usu�rio espec�fico.
    /// </summary>
    Task<IEnumerable<Category>> GetAllByUserIdAsync(Guid userId);

    /// <summary>
    /// Adiciona uma nova categoria ao contexto do EF.
    /// </summary>
    Task AddAsync(Category category);

    /// <summary>
    /// Marca uma categoria como modificada no contexto do EF.
    /// </summary>
    void Update(Category category);

    /// <summary>
    /// Marca uma categoria como removida no contexto do EF.
    /// </summary>
    void Delete(Category category);

    /// <summary>
    /// Verifica se uma categoria possui transa��es associadas.
    /// </summary>
    Task<bool> HasTransactionsAsync(Guid categoryId);

    /// <summary>
    /// Salva todas as mudan�as (Add, Update, Delete) no banco de dados.
    /// </summary>
    Task<bool> SaveChangesAsync();

    Task<Category?> GetByNameAsync(string name, Guid userId);
}