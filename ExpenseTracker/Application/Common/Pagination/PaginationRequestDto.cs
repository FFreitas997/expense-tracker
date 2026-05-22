namespace Application.Common.Pagination;

public abstract class PaginationRequestDto
{
    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;

    public string SortBy { get; set; } = string.Empty;

    public string SortOrder { get; set; } = "asc";
}