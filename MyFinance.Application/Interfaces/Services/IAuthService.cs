using MyFinance.Application.Dtos;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Services;

public interface IAuthService
{
    Task<(bool Success, LoginResponseDto? Data, string? ErrorMessage)> AuthenticateAsync(LoginRequestDto loginRequest);
    Task<(bool Success, string? ErrorMessage)> RegisterAsync(RegisterRequestDto registerRequest);
}