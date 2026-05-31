namespace Infrastructure.Repositories.Queries.Category;

public class CategoryQuery : PaginationQuery
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Constrains sorting to the fields exposed by Category.
    ///     When null, no sort is applied.
    /// </summary>
    public CategorySortBy? SortBy { get; set; }
}