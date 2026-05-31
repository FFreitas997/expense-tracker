using Infrastructure.Repositories.Queries.Category;

namespace Application.DTOs.Category;

public class CategorySearchRequestDto : PaginationRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Constrains sorting to the fields exposed by Category.
    ///     When null, no sort is applied.
    /// </summary>
    public CategorySortBy? SortBy { get; set; }

    public CategoryQuery ToCategoryQuery()
    {
        var query = new CategoryQuery
        {
            Name = Name,
            SortBy = SortBy
        };
        MapTo(query);
        return query;
    }
}