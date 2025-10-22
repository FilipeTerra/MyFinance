using MyFinance.Domain.Entities;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> CheckEmailExistsAsync(string email);
    Task AddUserAsync(User user);
}