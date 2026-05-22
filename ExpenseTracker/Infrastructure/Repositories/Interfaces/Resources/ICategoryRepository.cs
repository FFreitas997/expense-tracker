using Domain.Entities;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface ICategoryRepository : IRepository<Category, Guid>
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
}