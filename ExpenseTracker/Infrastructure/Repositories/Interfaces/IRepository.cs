namespace Infrastructure.Repositories.Interfaces;

/// <summary>
/// Generic repository contract that exposes core CRUD operations for any <typeparamref name="TEntity"/>.
/// All domain-specific repositories should extend this interface to avoid duplicating common data-access members.
/// </summary>
/// <typeparam name="TEntity">The domain entity type managed by this repository. Must be a reference type.</typeparam>
/// <typeparam name="TKey">The type of the primary key used to identify <typeparamref name="TEntity"/> records.</typeparam>
public interface IRepository<TEntity, in TKey> where TEntity : class
{
    /// <summary>
    /// Adds a new <typeparamref name="TEntity"/> to the data store.
    /// </summary>
    /// <param name="entity">The entity instance to persist.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The created entity, potentially enriched with database-generated values (e.g. generated keys).</returns>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Applies changes made to an existing <typeparamref name="TEntity"/> in the data store.
    /// </summary>
    /// <param name="entity">The entity instance containing the updated values.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The updated entity as reflected in the data store.</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single <typeparamref name="TEntity"/> by its primary key.
    /// </summary>
    /// <param name="id">The primary key value of the entity to retrieve.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>
    /// The matching entity, or <see langword="null"/> if no record with the given <paramref name="id"/> exists.
    /// </returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);

    /// <summary>
    /// Removes the specified <typeparamref name="TEntity"/> from the data store.
    /// </summary>
    /// <param name="entity">The entity instance to delete.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);
}