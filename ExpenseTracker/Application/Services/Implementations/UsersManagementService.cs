using Application.Common;
using Application.DTOs.User;
using Application.Services.Interfaces;
using Infrastructure.Cache.Interfaces;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class UsersManagementService(
    ILogger<UsersManagementService> logger,
    IUnitOfWork unit,
    ICacheRepository cache
) : IUsersManagementService
{
    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<UserDto>> UpdateUserAsync(UpdateUserRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}