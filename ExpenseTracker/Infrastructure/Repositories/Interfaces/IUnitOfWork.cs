using System.Data;
using Infrastructure.Repositories.Interfaces.Resources;

namespace Infrastructure.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IUserRepository Users { get; }
    IExpenseRepository Expenses { get; }
    ICategoryRepository Categories { get; }
    IBudgetRepository Budgets { get; }
    IRecurringExpenseRepository RecurringExpenses { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        IsolationLevel level = IsolationLevel.ReadCommitted,
        CancellationToken ct = default
    );

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel level = IsolationLevel.ReadCommitted,
        CancellationToken ct = default
    );
}