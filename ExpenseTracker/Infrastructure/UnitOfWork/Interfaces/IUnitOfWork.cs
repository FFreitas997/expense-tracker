using System.Data;
using Infrastructure.Repositories.Interfaces.Resources;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.UnitOfWork.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IUserRepository Users { get; }
    IExpenseRepository Expenses { get; }
    ICategoryRepository Categories { get; }
    IBudgetRepository Budgets { get; }
    IRecurringExpenseRepository RecurringExpenses { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel level, CancellationToken ct = default);
}