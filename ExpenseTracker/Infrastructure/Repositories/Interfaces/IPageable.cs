using Infrastructure.Repositories.Queries;

namespace Infrastructure.Repositories.Interfaces;

public interface IPageable<TEntity, in TRequest> where TRequest : PaginationQuery
{
    Task<PaginationResult<TEntity>> Search(TRequest req, CancellationToken ct = default);
}