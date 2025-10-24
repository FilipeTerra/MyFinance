using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;
using MyFinance.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ITransactionRepository _transactionRepository; // Para HasTransactionsAsync

    public CategoryRepository(ApplicationDbContext context, ITransactionRepository transactionRepository)
    {
        _context = context;
        _transactionRepository = transactionRepository;
    }

    public async Task<Category?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }

    public async Task<IEnumerable<Category>> GetAllByUserIdAsync(Guid userId)
    {
        return await _context.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public void Update(Category category)
    {
        _context.Categories.Update(category);
    }

    public void Delete(Category category)
    {
        _context.Categories.Remove(category);
    }

    public async Task<bool> HasTransactionsAsync(Guid categoryId)
    {
        // Precisamos verificar se existe ALGUMA transação para a categoria
        // Esta lógica depende de como TransactionRepository implementa a verificação.
        // Se TransactionRepository.HasTransactionsAsync for específico da *conta*,
        // precisamos de uma verificação direta aqui:
        return await _context.Transactions.AnyAsync(t => t.CategoryId == categoryId);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}