using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;
using MyFinance.Application.Dtos;

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
        // Busca a transa��o e inclui a conta, 
        // depois verifica se a conta pertence ao usu�rio.
        return await _context.Transactions
            .Include(t => t.Account) // Inclui os dados da conta relacionada
            .Include(t => t.Category) // Inclui os dados da categoria relacionada
            .FirstOrDefaultAsync(t => t.Id == id && t.Account.UserId == userId);
    }

    public async Task<IEnumerable<Transaction>> GetAllByAccountIdAsync(Guid accountId, Guid userId)
    {
        // Verifica se a conta pertence ao usu�rio
        var accountExists = await _context.Accounts
                                .AnyAsync(a => a.Id == accountId && a.UserId == userId);

        if (!accountExists)
        {
            return Enumerable.Empty<Transaction>();
        }

        // Busca as transa��es da conta, ordenadas pela data (mais recente primeiro)
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
        // Garante que a conta pertence ao usu�rio E que estamos buscando na conta correta
        var query = _context.Transactions
            .Where(t => t.Account.UserId == userId && t.AccountId == filters.AccountId);

        // Filtro de Descri��o Textual
        if (!string.IsNullOrWhiteSpace(filters.SearchText))
        {
            // Usando EF.Functions.ILike para busca case-insensitive (espec�fico do PostgreSQL)
            // Se usar SQL Server, seria t.Description.Contains(filters.SearchText)
            // Para ser mais gen�rico e case-insensitive:
            string searchTextLower = filters.SearchText.ToLower();
            query = query.Where(t => t.Description.ToLower().Contains(searchTextLower));
        }

        // Filtro de Valor
        if (filters.Amount.HasValue)
        {
            query = query.Where(t => t.Amount == filters.Amount.Value);
        }

        // Filtro de Data
        if (filters.Date.HasValue)
        {
            // O banco armazena como 'date' (sem hora)
            var dateToFilter = filters.Date.Value.Date;
            query = query.Where(t => t.Date == dateToFilter);
        }

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
            await _context.SaveChangesAsync();
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
        // Verifica se existe ALGUMA transa��o para a conta
        return await _context.Transactions.AnyAsync(t => t.AccountId == accountId);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}