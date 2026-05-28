using Domain.Enums;
using Infrastructure.Repositories.Queries.Enums;

namespace Infrastructure.Repositories.Queries.User;

public class UserQuery : PaginationQuery
{
    public string FullName { get; set; } = string.Empty;

    public UserRole? Role { get; set; }

    public UserState? State { get; set; }

    /// <summary>
    ///     Constrains sorting to the fields exposed by User.
    ///     When null, no sort is applied.
    /// </summary>
    public UserSortBy? SortBy { get; set; }
}