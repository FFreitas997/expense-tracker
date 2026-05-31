using Domain.Entities;
using Infrastructure.Repositories.Interfaces.Resources;
using Infrastructure.Repositories.Queries;
using Infrastructure.Repositories.Queries.Category;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class CategoryRepository(ILogger<CategoryRepository> logger, AppDbContext dbContext) : ICategoryRepository
{
    public async Task<Category> CreateAsync(Category entity, CancellationToken ct = default)
    {
        logger.LogInformation("Creating category with ID {CategoryId}", entity.Id);
        await dbContext.Categories.AddAsync(entity, ct);
        return entity;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching category with ID {CategoryId}", id);
        return await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Category> UpdateAsync(Category entity, CancellationToken ct = default)
    {
        logger.LogInformation("Updating category with ID {CategoryId}", entity.Id);
        dbContext.Categories.Update(entity);

        return await Task.FromResult(entity);
    }

    public Task DeleteAsync(Category entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Invalid category ID.", nameof(entity));

        logger.LogInformation("Deleting category with ID {CategoryId}", entity.Id);
        dbContext.Categories.Remove(entity);

        return Task.CompletedTask;
    }

    public async Task<PaginationResult<Category>> Search(CategoryQuery req, CancellationToken ct = default)
    {
        logger.LogInformation("Searching categories with query {Query}", req);
        var query = dbContext.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            var search = $"%{req.Name.ToLowerInvariant().Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.Name, search));
        }

        if (req.SortBy.HasValue)
            query = req.SortBy.Value switch
            {
                CategorySortBy.Name => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(c => c.Name)
                    : query.OrderByDescending(c => c.Name),
                _ => query
            };

        var totalItems = await query.CountAsync(ct);
        var content = await query
            .Skip((req.Page - 1) * req.Size)
            .Take(req.Size)
            .ToListAsync(ct);

        return new PaginationResult<Category>
        {
            TotalItems = totalItems,
            Items = content,
            Page = req.Page,
            Size = req.Size
        };
    }
}