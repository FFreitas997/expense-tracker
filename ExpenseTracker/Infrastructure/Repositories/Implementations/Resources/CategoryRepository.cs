using Domain.Entities;
using Infrastructure.Repositories.Interfaces.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class CategoryRepository(ILogger<CategoryRepository> logger, AppDbContext dbContext) : ICategoryRepository
{
    public async Task<Category?> CreateAsync(Category entity, CancellationToken ct = default)
    {
        logger.LogDebug("Creating category with name: {Name}", entity.Name);

        await dbContext.Categories.AddAsync(entity, ct);

        //cache.Remove(CacheKeyAll);

        return entity;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogDebug("Getting category with ID: {Id}", id);

        return await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Category?> UpdateAsync(Category entity, CancellationToken ct = default)
    {
        logger.LogDebug("Updating category with ID: {Id}", entity.Id);

        var cat = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);

        if (cat is null) return null;

        cat.Name = entity.Name;
        cat.Icon = entity.Icon;
        cat.Color = entity.Color;
        cat.IsDefault = entity.IsDefault;

        cat.ModifiedAt = DateTime.UtcNow;
        cat.ModifiedBy = entity.ModifiedBy;

        dbContext.Categories.Update(cat);

        //cache.Remove(CacheKey(entity.Id));
        //cache.Remove(CacheKeyAll);

        return cat;
    }

    public Task DeleteAsync(Category entity, CancellationToken ct = default)
    {
        logger.LogDebug(" Deleting category with ID: {Id}", entity.Id);

        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Invalid category ID");

        dbContext.Categories.Remove(entity);

        //cache.Remove(CacheKey(entity.Id));
        //cache.Remove(CacheKeyAll);

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbContext.Categories.AsNoTracking().ToListAsync(ct);
    }
}