using Microsoft.EntityFrameworkCore;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Domain.Entities;
using System.Threading.Tasks;

namespace MyFinance.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        // Verifica eficientemente se algum usuário com o email existe
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task AddUserAsync(User user)
    {
        // Adiciona o novo usuário ao contexto
        await _context.Users.AddAsync(user);
        // Salva as mudanças no banco de dados
        await _context.SaveChangesAsync();
    }
}