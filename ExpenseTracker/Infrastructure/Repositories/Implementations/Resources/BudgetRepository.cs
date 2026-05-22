using Domain.Entities;
using Infrastructure.Repositories.Interfaces.Resources;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations.Resources;

public class BudgetRepository(ILogger<BudgetRepository> logger, AppDbContext dbContext) : IBudgetRepository
{
    public async Task<Budget?> CreateAsync(Budget entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Budget?> UpdateAsync(Budget entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Budget?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(Budget entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}