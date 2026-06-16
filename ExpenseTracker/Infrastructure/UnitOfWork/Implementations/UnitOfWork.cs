using Infrastructure.Data;
using Infrastructure.Repositories.Interfaces.Resources;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using IsolationLevel = System.Data.IsolationLevel;

namespace Infrastructure.UnitOfWork.Implementations;

/// <summary>
///     Concrete implementation of <see cref="IUnitOfWork" /> that coordinates a single <see cref="AppDbContext" />
///     instance shared across all repositories, ensuring that every operation within a business
///     transaction is committed or rolled back atomically.
/// </summary>
/// <param name="context">The EF Core database context shared by all repositories.</param>
/// <param name="users">Repository for <c>User</c> aggregate operations.</param>
/// <param name="expenses">Repository for <c>Expense</c> aggregate operations.</param>
/// <param name="categories">Repository for <c>Category</c> aggregate operations.</param>
/// <param name="budgets">Repository for <c>Budget</c> aggregate operations.</param>
/// <param name="recurringExpenses">Repository for <c>RecurringExpense</c> aggregate operations.</param>
public class UnitOfWork(
    AppDbContext context,
    IUserRepository users,
    IExpenseRepository expenses,
    ICategoryRepository categories,
    IBudgetRepository budgets,
    IRecurringExpenseRepository recurringExpenses
) : IUnitOfWork
{
    // Tracks whether the instance has already been disposed to prevent double-disposal.
    private bool _disposed;

    /// <summary>Gets the repository for user-related data access.</summary>
    public IUserRepository Users { get; } = users;

    /// <summary>Gets the repository for expense-related data access.</summary>
    public IExpenseRepository Expenses { get; } = expenses;

    /// <summary>Gets the repository for category-related data access.</summary>
    public ICategoryRepository Categories { get; } = categories;

    /// <summary>Gets the repository for budget-related data access.</summary>
    public IBudgetRepository Budgets { get; } = budgets;

    /// <summary>Gets the repository for recurring-expense-related data access.</summary>
    public IRecurringExpenseRepository RecurringExpenses { get; } = recurringExpenses;

    /// <summary>
    ///     Persists all pending changes tracked by the shared <see cref="AppDbContext" /> to the database.
    /// </summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await context.SaveChangesAsync(ct);
    }

    /// <summary>
    ///     Begins a new database transaction with the specified isolation level,
    ///     allowing multiple save operations to be committed or rolled back as a single unit.
    /// </summary>
    /// <param name="level">The isolation level for the transaction.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The started <see cref="IDbContextTransaction" />.</returns>
    public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel level, CancellationToken ct = default)
    {
        return await context.Database.BeginTransactionAsync(level, ct);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Asynchronously releases the underlying <see cref="AppDbContext" /> and suppresses finalization.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await context.DisposeAsync();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Releases managed resources when <paramref name="disposing" /> is <see langword="true" />.
    /// </summary>
    /// <param name="disposing">
    ///     <see langword="true" /> when called from <see cref="Dispose()" />; <see langword="false" /> when called from a
    ///     finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
            context.Dispose();

        _disposed = true;
    }
}