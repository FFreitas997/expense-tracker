using Domain.Entities;
using Infrastructure.Repositories.Interfaces.Resources;
using Infrastructure.Repositories.Queries;
using Infrastructure.Repositories.Queries.Enums;
using Infrastructure.Repositories.Queries.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class UserRepository(ILogger<UserRepository> logger, AppDbContext dbContext) : IUserRepository
{
    public async Task<User> CreateAsync(User entity, CancellationToken ct = default)
    {
        logger.LogInformation("Creating user with ID {UserId}", entity.Id);
        await dbContext.Users.AddAsync(entity, ct);
        return entity;
    }

    public async Task<User> UpdateAsync(User entity, CancellationToken ct = default)
    {
        logger.LogInformation("Updating user with ID {UserId}", entity.Id);
        dbContext.Users.Update(entity);
        return await Task.FromResult(entity);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching user with ID {UserId}", id);
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public Task DeleteAsync(User entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Invalid user ID.", nameof(entity));

        logger.LogInformation("Deleting user with ID {UserId}", entity.Id);
        dbContext.Users.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<PaginationResult<User>> Search(UserQuery req, CancellationToken ct = default)
    {
        logger.LogInformation("Searching users with query {Query}", req);
        var query = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.FullName))
        {
            var search = $"%{req.FullName.ToLowerInvariant().Trim()}%";
            query = query.Where(u => EF.Functions.Like(u.FullName, search));
        }

        if (req.Role.HasValue)
            query = query.Where(u => u.Role == req.Role.Value);

        if (req.State.HasValue)
            query = query.Where(u => u.State == req.State.Value);

        if (req.SortBy.HasValue)
            query = req.SortBy.Value switch
            {
                UserSortBy.FullName => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(u => u.FullName)
                    : query.OrderByDescending(u => u.FullName),
                UserSortBy.CreatedAt => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(u => u.CreatedAt)
                    : query.OrderByDescending(u => u.CreatedAt),
                UserSortBy.LastLogin => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(u => u.LastLogin)
                    : query.OrderByDescending(u => u.LastLogin),
                _ => query
            };

        var totalItems = await query.CountAsync(ct);
        var content = await query
            .Skip((req.Page - 1) * req.Size)
            .Take(req.Size)
            .ToListAsync(ct);

        return new PaginationResult<User>
        {
            TotalItems = totalItems,
            Items = content,
            Page = req.Page,
            Size = req.Size
        };
    }
}