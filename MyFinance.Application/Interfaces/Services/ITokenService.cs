using MyFinance.Domain.Entities;

namespace MyFinance.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
