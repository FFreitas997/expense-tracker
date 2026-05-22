using Domain.Entities;
using Infrastructure.Repositories.Interfaces.Resources;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class RecurringExpenseRepository(ILogger<RecurringExpenseRepository> logger, AppDbContext dbContext)
    : IRecurringExpenseRepository
{
    public async Task<RecurringExpense?> CreateAsync(RecurringExpense entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<RecurringExpense?> UpdateAsync(RecurringExpense entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<RecurringExpense?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(RecurringExpense entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}