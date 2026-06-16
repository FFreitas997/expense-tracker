using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Interfaces.Resources;
using Infrastructure.Repositories.Queries;
using Infrastructure.Repositories.Queries.Enums;
using Infrastructure.Repositories.Queries.Expense;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class ExpenseRepository(ILogger<ExpenseRepository> logger, AppDbContext dbContext) : IExpenseRepository
{
    public async Task<Expense> CreateAsync(Expense entity, CancellationToken ct = default)
    {
        logger.LogInformation("Creating expense with ID {ExpenseId}", entity.Id);
        await dbContext.Expenses.AddAsync(entity, ct);
        return entity;
    }

    public async Task<Expense> UpdateAsync(Expense entity, CancellationToken ct = default)
    {
        logger.LogInformation("Updating expense with ID {ExpenseId}", entity.Id);
        dbContext.Expenses.Update(entity);
        return await Task.FromResult(entity);
    }

    public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching expense with ID {ExpenseId}", id);
        return await dbContext.Expenses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public Task DeleteAsync(Expense entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Invalid expense ID.", nameof(entity));

        logger.LogInformation("Deleting expense with ID {ExpenseId}", entity.Id);
        dbContext.Expenses.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<PaginationResult<Expense>> Search(ExpenseQuery req, CancellationToken ct = default)
    {
        logger.LogInformation("Searching expenses with query {Query}", req);
        var query = dbContext.Expenses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Description))
        {
            var search = $"%{req.Description.ToLowerInvariant().Trim()}%";
            query = query.Where(e => EF.Functions.Like(e.Description, search));
        }

        if (req.MinAmount.HasValue)
            query = query.Where(e => e.Amount >= req.MinAmount.Value);

        if (req.MaxAmount.HasValue)
            query = query.Where(e => e.Amount <= req.MaxAmount.Value);

        if (req.DateFrom.HasValue)
            query = query.Where(e => e.Date >= req.DateFrom.Value);

        if (req.DateTo.HasValue)
            query = query.Where(e => e.Date <= req.DateTo.Value);

        if (req.PaymentMethod.HasValue)
            query = query.Where(e => e.PaymentMethod == req.PaymentMethod.Value);

        if (req.SortBy.HasValue)
            query = req.SortBy.Value switch
            {
                ExpenseSortBy.Amount => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(e => e.Amount)
                    : query.OrderByDescending(e => e.Amount),
                ExpenseSortBy.Date => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(e => e.Date)
                    : query.OrderByDescending(e => e.Date),
                ExpenseSortBy.Description => req.SortOrder == SortOrder.Asc
                    ? query.OrderBy(e => e.Description)
                    : query.OrderByDescending(e => e.Description),
                _ => query
            };

        var totalItems = await query.CountAsync(ct);
        var content = await query
            .Skip((req.Page - 1) * req.Size)
            .Take(req.Size)
            .ToListAsync(ct);

        return new PaginationResult<Expense>
        {
            TotalItems = totalItems,
            Items = content,
            Page = req.Page,
            Size = req.Size
        };
    }
}