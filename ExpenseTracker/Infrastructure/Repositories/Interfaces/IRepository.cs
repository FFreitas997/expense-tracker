namespace Infrastructure.Repositories.Interfaces;

//CRUD operations for all repositories, to avoid code duplication.
public interface IRepository<TEntity, in TKey> where TEntity : class
{
    Task<TEntity?> CreateAsync(TEntity entity, CancellationToken ct = default);

    Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken ct = default);

    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);

    Task DeleteAsync(TEntity entity, CancellationToken ct = default);
}