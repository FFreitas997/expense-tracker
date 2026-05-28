using Infrastructure.Repositories.Interfaces;
using Infrastructure.Repositories.Interfaces.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel level, CancellationToken ct = default)
    {
        return await context.Database.BeginTransactionAsync(level, ct);
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