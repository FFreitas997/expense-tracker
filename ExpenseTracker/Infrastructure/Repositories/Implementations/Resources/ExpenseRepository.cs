using Domain.Entities;
using Infrastructure.Repositories.Interfaces.Resources;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class ExpenseRepository(ILogger<ExpenseRepository> logger, AppDbContext dbContext) : IExpenseRepository
{
    public async Task<Expense?> CreateAsync(Expense entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Expense?> UpdateAsync(Expense entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(Expense entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}