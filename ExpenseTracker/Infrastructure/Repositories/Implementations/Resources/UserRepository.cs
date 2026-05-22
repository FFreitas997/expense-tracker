using Domain.Entities;
using Infrastructure.Repositories.Interfaces.Resources;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class UserRepository(ILogger<UserRepository> logger, AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> CreateAsync(User entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<User?> UpdateAsync(User entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(User entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}