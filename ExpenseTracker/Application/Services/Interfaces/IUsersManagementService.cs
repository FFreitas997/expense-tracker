using Application.Common;
using Application.DTOs.User;

namespace Application.Services.Interfaces;

public interface IUsersManagementService
{
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequestDto request, CancellationToken ct = default);

    Task<Result<UserDto>> UpdateUserAsync(UpdateUserRequestDto request, CancellationToken ct = default);

    Task<Result> DeleteUserAsync(Guid userId, CancellationToken ct = default);

    Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
}