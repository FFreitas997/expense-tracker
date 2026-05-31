using Domain.Entities;
using Infrastructure.Repositories.Interfaces.Resources;
using Infrastructure.Repositories.Queries;
using Infrastructure.Repositories.Queries.RecurringExpense;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class RecurringExpenseRepository(ILogger<RecurringExpenseRepository> logger, AppDbContext dbContext)
    : IRecurringExpenseRepository
{
    public async Task<RecurringExpense> CreateAsync(RecurringExpense entity, CancellationToken ct = default)
    {
        logger.LogInformation("Creating recurring expense with ID {RecurringExpenseId}", entity.Id);
        await dbContext.RecurringExpenses.AddAsync(entity, ct);
        return entity;
    }

    public async Task<RecurringExpense> UpdateAsync(RecurringExpense entity, CancellationToken ct = default)
    {
        logger.LogInformation("Updating recurring expense with ID {RecurringExpenseId}", entity.Id);
        dbContext.RecurringExpenses.Update(entity);
        return await Task.FromResult(entity);
    }

    public async Task<RecurringExpense?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching recurring expense with ID {RecurringExpenseId}", id);
        return await dbContext.RecurringExpenses
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public Task DeleteAsync(RecurringExpense entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Invalid recurring expense ID.", nameof(entity));

        logger.LogInformation("Deleting recurring expense with ID {RecurringExpenseId}", entity.Id);
        dbContext.RecurringExpenses.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<PaginationResult<RecurringExpense>> Search(RecurringExpenseQuery req,
        CancellationToken ct = default)
    {
        logger.LogInformation("Searching recurring expenses with query {Query}", req);
        var query = dbContext.RecurringExpenses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Description))
        {
            var search = $"%{req.Description.ToLowerInvariant().Trim()}%";
            query = query.Where(r => EF.Functions.Like(r.Description, search));
        }

        if (req.IsActive.HasValue)
            query = query.Where(r => r.IsActive == req.IsActive.Value);

        if (req.Frequency.HasValue)
            query = query.Where(r => r.Frequency == req.Frequency.Value);

        if (req.SortBy.HasValue)
            query = req.SortBy.Value switch
            {
                RecurringExpenseSortBy.Amount => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(r => r.Amount)
                    : query.OrderByDescending(r => r.Amount),
                RecurringExpenseSortBy.NextDueDate => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(r => r.NextDueDate)
                    : query.OrderByDescending(r => r.NextDueDate),
                RecurringExpenseSortBy.Description => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(r => r.Description)
                    : query.OrderByDescending(r => r.Description),
                _ => query
            };

        var totalItems = await query.CountAsync(ct);
        var content = await query
            .Skip((req.Page - 1) * req.Size)
            .Take(req.Size)
            .ToListAsync(ct);

        return new PaginationResult<RecurringExpense>
        {
            TotalItems = totalItems,
            Items = content,
            Page = req.Page,
            Size = req.Size
        };
    }

    public async Task<List<RecurringExpense>> GetDueAsync(DateTime asOf, CancellationToken ct = default)
    {
        return await dbContext.RecurringExpenses
            .Where(r => r.IsActive && r.NextDueDate <= asOf)
            .Include(r => r.Category)
            .ToListAsync(ct);
    }
}