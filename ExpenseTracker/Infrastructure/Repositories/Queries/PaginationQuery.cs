using Infrastructure.Repositories.Queries.Enums;

namespace Infrastructure.Repositories.Queries;

public abstract class PaginationQuery
{
    private int _page = 1;

    private int _size = 10;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int Size
    {
        get => _size;
        set => _size = value is < 1 or > 500 ? 10 : value;
    }

    public SortOrder SortOrder { get; set; } = SortOrder.Asc;
}