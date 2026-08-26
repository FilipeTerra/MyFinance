using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Interfaces.Repositories
{
    public interface ICotacaoHistoricoRepository
    {
        Task AddAsync(CotacaoHistorico cotacao);

        Task<bool> ExisteParaDataAsync(Guid investimentoId, DateTime data);

        /// <summary>
        /// Verifica se o investimento já possui algum ponto de cotação registrado
        /// (usado para decidir se é necessário fazer o backfill do histórico).
        /// </summary>
        Task<bool> ExisteAlgumRegistroAsync(Guid investimentoId);

        /// <summary>
        /// Retorna o ponto de cotação mais recente do investimento, ou null se não houver nenhum.
        /// Usado para calcular a variação percentual de mercado a aplicar sobre o ValorAtual.
        /// </summary>
        Task<CotacaoHistorico?> GetUltimaCotacaoAsync(Guid investimentoId);

        Task<IEnumerable<CotacaoHistorico>> GetByInvestimentoIdsSinceAsync(IEnumerable<Guid> investimentoIds, DateTime desde);

        Task<bool> SaveChangesAsync();
    }
}
