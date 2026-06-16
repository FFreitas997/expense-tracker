using Domain.Entities;
using Infrastructure.Repositories.Queries.Category;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface ICategoryRepository : IRepository<Category, Guid>, IPageable<Category, CategoryQuery>
{
    Task<List<Category>> GetAllForUserAsync(Guid userId, CancellationToken ct = default);

    Task<List<Category>> GetAllSystemAsync(CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, Guid? userId, CancellationToken ct = default);

    Task<bool> HasLinkedExpensesAsync(Guid id, CancellationToken ct = default);
}