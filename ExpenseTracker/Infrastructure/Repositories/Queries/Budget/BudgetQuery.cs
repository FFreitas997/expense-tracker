using Domain.Enums;
using Infrastructure.Repositories.Queries.Enums;

namespace Infrastructure.Repositories.Queries.Budget;

public class BudgetQuery : PaginationQuery
{
    public BudgetPeriod? Period { get; set; }

    public DateTime? StartDateFrom { get; set; }

    public DateTime? StartDateTo { get; set; }

    /// <summary>
    ///     Constrains sorting to the fields exposed by Budget.
    ///     When null, no sort is applied.
    /// </summary>
    public BudgetSortBy? SortBy { get; set; }
}