using MyFinance.Application.Dtos;

namespace MyFinance.Application.Interfaces.Services;

public interface IUserService
{
    Task<ServiceResponse<UserProfileResponseDto>> GetProfileAsync(Guid userId);
    Task<ServiceResponse<UserProfileResponseDto>> UpdateProfileAsync(Guid userId, UpdateUserProfileRequestDto request);
}
