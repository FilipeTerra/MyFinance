using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;

namespace MyFinance.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, Guid userId)
    {
        // Busca a transação e inclui a conta, 
        // depois verifica se a conta pertence ao usuário.
        return await _context.Transactions
            .Include(t => t.Account) // Inclui os dados da conta relacionada
            .Include(t => t.Category) // Inclui os dados da categoria relacionada
            .FirstOrDefaultAsync(t => t.Id == id && t.Account.UserId == userId);
    }

    public async Task<IEnumerable<Transaction>> GetAllByAccountIdAsync(Guid accountId, Guid userId)
    {
        // Verifica se a conta pertence ao usuário
        var accountExists = await _context.Accounts
                                .AnyAsync(a => a.Id == accountId && a.UserId == userId);

        if (!accountExists)
        {
            return Enumerable.Empty<Transaction>();
        }

        // Busca as transações da conta, ordenadas pela data (mais recente primeiro)
        return await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Include(t => t.Account)
            .Include(t => t.Category)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByFilterAsync(Guid userId, TransactionSearchRequestDto filters)
    {
        // Garante que a conta pertence ao usuário E que estamos buscando na conta correta
        var query = _context.Transactions
            .Where(t => t.Account.UserId == userId && t.AccountId == filters.AccountId);

        // Filtro de Descrição Textual
        if (!string.IsNullOrWhiteSpace(filters.SearchText))
        {
            // Usando EF.Functions.ILike para busca case-insensitive (específico do PostgreSQL)
            // Se usar SQL Server, seria t.Description.Contains(filters.SearchText)
            // Para ser mais genérico e case-insensitive:
            string searchTextLower = filters.SearchText.ToLower();
            query = query.Where(t => t.Description.ToLower().Contains(searchTextLower));
        }

        // Filtro de Valor
        if (filters.Amount.HasValue)
        {
            query = query.Where(t => t.Amount == filters.Amount.Value);
        }

        // Filtro de Tipo (Receita / Despesa)
        if (filters.Type.HasValue)
        {
            query = query.Where(t => t.Type == filters.Type.Value);
        }

        // Filtro de Categoria
        if (filters.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == filters.CategoryId.Value);
        }

        // Filtro de Período (range de datas)
        if (filters.StartDate.HasValue)
            query = query.Where(t => t.Date >= filters.StartDate.Value.Date);

        if (filters.EndDate.HasValue)
            query = query.Where(t => t.Date <= filters.EndDate.Value.Date);

        // Incluir entidades relacionadas (essencial para o Mapeamento)
        query = query.Include(t => t.Account)
                     .Include(t => t.Category);

        // Ordenar (igual ao GetAllByAccountIdAsync)
        query = query.OrderByDescending(t => t.Date)
                     .ThenByDescending(t => t.CreatedAt);

        // Pagina��o (Opcional, mas recomendado. O DTO j� suporta)
        query = query.Skip((filters.Page - 1) * filters.PageSize)
                     .Take(filters.PageSize);

        return await query.ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
    {
        await _context.Transactions.AddRangeAsync(transactions);
    }

    public async Task<IEnumerable<Transaction>> GetByFinancialGoalIdAsync(Guid goalId)
    {
        return await _context.Transactions
            .Where(t => t.FinancialGoalId == goalId)
            .Include(t => t.Account)
            .ToListAsync();
    }

    public async Task AddAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
    }

    public void Update(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
    }

    public void Delete(Transaction transaction)
    {
        _context.Transactions.Remove(transaction);
    }

    public async Task<bool> HasTransactionsAsync(Guid accountId)
    {
        // Verifica se existe ALGUMA transação para a conta
        return await _context.Transactions.AnyAsync(t => t.AccountId == accountId);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<ITransactionDbTransaction> BeginTransactionAsync()
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        return new EfCoreTransaction(transaction);
    }

    private sealed class EfCoreTransaction : ITransactionDbTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfCoreTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync() => _transaction.CommitAsync();

        public Task RollbackAsync() => _transaction.RollbackAsync();

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}