using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;
using MyFinance.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
}