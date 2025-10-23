using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;
using MyFinance.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _context;

    public AccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id, Guid userId)
    {
        // Busca a conta por ID, MAS garante que ela pertence ao usuário logado
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }

    public async Task<IEnumerable<Account>> GetAllByUserIdAsync(Guid userId)
    {
        // Busca todas as contas do usuário logado
        return await _context.Accounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
    }

    public void Update(Account account)
    {
        // O EF Core rastreia a entidade e a marca como 'Modificada'
        _context.Accounts.Update(account);
    }

    public void Delete(Account account)
    {
        _context.Accounts.Remove(account);
    }

    public async Task<bool> SaveChangesAsync()
    {
        // Salva todas as mudanças (Add, Update, Delete) no banco
        // Retorna true se pelo menos 1 linha foi afetada
        return await _context.SaveChangesAsync() > 0;
    }
}