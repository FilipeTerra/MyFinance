using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ServiceResponse<UserProfileResponseDto>> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
            return new ServiceResponse<UserProfileResponseDto> { Success = false, ErrorMessage = "Usuário não encontrado." };

        return new ServiceResponse<UserProfileResponseDto>
        {
            Data = new UserProfileResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                MonthlyIncome = user.MonthlyIncome
            }
        };
    }

    public async Task<ServiceResponse<UserProfileResponseDto>> UpdateProfileAsync(Guid userId, UpdateUserProfileRequestDto request)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
            return new ServiceResponse<UserProfileResponseDto> { Success = false, ErrorMessage = "Usuário não encontrado." };

        user.MonthlyIncome = request.MonthlyIncome;
        await _userRepository.UpdateUserAsync(user);

        return new ServiceResponse<UserProfileResponseDto>
        {
            Data = new UserProfileResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                MonthlyIncome = user.MonthlyIncome
            }
        };
    }
}
