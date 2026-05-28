namespace Infrastructure.Repositories.Queries;

public class PaginationResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int TotalItems { get; set; }

    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;

    public int TotalPages => Size > 0 ? (int)Math.Ceiling(TotalItems / (double)Size) : 0;

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1;
}