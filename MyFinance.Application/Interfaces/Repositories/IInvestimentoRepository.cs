using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Interfaces.Repositories
{
    public interface IInvestimentoRepository
    {
        Task<Investimento> GetByIdAsync(Guid id);
        Task<IEnumerable<Investimento>> GetAllByUserIdAsync(Guid userId);

        /// <summary>
        /// Adiciona o investimento ao contexto do EF (não persiste — chame SaveChangesAsync).
        /// </summary>
        Task AddAsync(Investimento investimento);

        /// <summary>
        /// Marca o investimento como modificado no contexto (não persiste).
        /// </summary>
        void Update(Investimento investimento);

        /// <summary>
        /// Marca o investimento como removido no contexto (não persiste).
        /// </summary>
        void Delete(Investimento investimento);

        /// <summary>
        /// Persiste todas as mudanças pendentes no banco de dados.
        /// </summary>
        Task<bool> SaveChangesAsync();
    }
}
