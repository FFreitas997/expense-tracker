using Infrastructure.Repositories.Queries;
using Infrastructure.Repositories.Queries.Enums;

namespace Application.DTOs;

public abstract class PaginationRequest
{
    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;

    public SortOrder SortOrder { get; set; } = SortOrder.Asc;

    protected void MapTo(PaginationQuery query)
    {
        query.Page = Page;
        query.Size = Size;
        query.SortOrder = SortOrder;
    }
}