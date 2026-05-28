using Domain.Entities;
using Infrastructure.Repositories.Interfaces.Resources;
using Infrastructure.Repositories.Queries;
using Infrastructure.Repositories.Queries.Budget;
using Infrastructure.Repositories.Queries.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class BudgetRepository(ILogger<BudgetRepository> logger, AppDbContext dbContext) : IBudgetRepository
{
    public async Task<Budget> CreateAsync(Budget entity, CancellationToken ct = default)
    {
        logger.LogInformation("Creating budget with ID {BudgetId}", entity.Id);
        await dbContext.Budgets.AddAsync(entity, ct);
        return entity;
    }

    public async Task<Budget> UpdateAsync(Budget entity, CancellationToken ct = default)
    {
        logger.LogInformation("Updating budget with ID {BudgetId}", entity.Id);
        dbContext.Budgets.Update(entity);
        return await Task.FromResult(entity);
    }

    public async Task<Budget?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching budget with ID {BudgetId}", id);
        return await dbContext.Budgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public Task DeleteAsync(Budget entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Invalid budget ID.", nameof(entity));

        logger.LogInformation("Deleting budget with ID {BudgetId}", entity.Id);
        dbContext.Budgets.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<PaginationResult<Budget>> Search(BudgetQuery req, CancellationToken ct = default)
    {
        logger.LogInformation("Searching budgets with query {Query}", req);
        var query = dbContext.Budgets.AsNoTracking();

        if (req.Period.HasValue)
            query = query.Where(b => b.Period == req.Period.Value);

        if (req.StartDateFrom.HasValue)
            query = query.Where(b => b.StartDate >= req.StartDateFrom.Value);

        if (req.StartDateTo.HasValue)
            query = query.Where(b => b.StartDate <= req.StartDateTo.Value);

        if (req.SortBy.HasValue)
            query = req.SortBy.Value switch
            {
                BudgetSortBy.LimitAmount => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(b => b.LimitAmount)
                    : query.OrderByDescending(b => b.LimitAmount),
                BudgetSortBy.StartDate => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(b => b.StartDate)
                    : query.OrderByDescending(b => b.StartDate),
                BudgetSortBy.Period => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(b => b.Period)
                    : query.OrderByDescending(b => b.Period),
                _ => query
            };

        var totalItems = await query.CountAsync(ct);
        var content = await query
            .Skip((req.Page - 1) * req.Size)
            .Take(req.Size)
            .ToListAsync(ct);

        return new PaginationResult<Budget>
        {
            TotalItems = totalItems,
            Items = content,
            Page = req.Page,
            Size = req.Size
        };
    }
}