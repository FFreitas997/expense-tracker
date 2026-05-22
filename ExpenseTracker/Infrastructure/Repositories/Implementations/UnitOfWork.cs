using Infrastructure.Repositories.Interfaces;
using Infrastructure.Repositories.Interfaces.Resources;
using Microsoft.EntityFrameworkCore;
using IsolationLevel = System.Data.IsolationLevel;

namespace Infrastructure.Repositories.Implementations;

public class UnitOfWork(
    AppDbContext context,
    IUserRepository users,
    IExpenseRepository expenses,
    ICategoryRepository categories,
    IBudgetRepository budgets,
    IRecurringExpenseRepository recurringExpenses
) : IUnitOfWork
{
    public IUserRepository Users { get; } = users;
    public IExpenseRepository Expenses { get; } = expenses;
    public ICategoryRepository Categories { get; } = categories;
    public IBudgetRepository Budgets { get; } = budgets;
    public IRecurringExpenseRepository RecurringExpenses { get; } = recurringExpenses;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await context.SaveChangesAsync(ct);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        IsolationLevel level = IsolationLevel.ReadCommitted,
        CancellationToken ct = default
    )
    {
        await using var transaction = await context.Database.BeginTransactionAsync(level, ct);

        try
        {
            await operation(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel level = IsolationLevel.ReadCommitted,
        CancellationToken ct = default
    )
    {
        await using var transaction = await context.Database.BeginTransactionAsync(level, ct);
        try
        {
            var result = await operation(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public void Dispose()
    {
        context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}