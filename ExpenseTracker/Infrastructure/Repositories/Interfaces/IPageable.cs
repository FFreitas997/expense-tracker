using Infrastructure.Repositories.Queries;

namespace Infrastructure.Repositories.Interfaces;

/// <summary>
/// Defines a paginated search capability for a repository that works with <typeparamref name="TEntity"/> records.
/// Repositories that support pagination should implement this interface alongside the base repository interface.
/// </summary>
/// <typeparam name="TEntity">The domain entity type returned in each page of results.</typeparam>
/// <typeparam name="TRequest">
/// The pagination query type carrying filter, sorting, and paging parameters.
/// Must derive from <see cref="PaginationQuery"/>.
/// </typeparam>
public interface IPageable<TEntity, in TRequest> where TRequest : PaginationQuery
{
    /// <summary>
    /// Executes a paginated search against the underlying data store using the criteria
    /// defined in <paramref name="req"/> and returns a single page of matching <typeparamref name="TEntity"/> results.
    /// </summary>
    /// <param name="req">The query object containing filter, sort, page index, and page size parameters.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="PaginationResult{TEntity}"/> containing the current page of items together
    /// with total count metadata needed to build pagination controls.
    /// </returns>
    Task<PaginationResult<TEntity>> Search(TRequest req, CancellationToken ct = default);
}