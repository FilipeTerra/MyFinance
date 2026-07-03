using MyFinance.Domain.Entities;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(Guid id);
    Task<bool> CheckEmailExistsAsync(string email);
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
}