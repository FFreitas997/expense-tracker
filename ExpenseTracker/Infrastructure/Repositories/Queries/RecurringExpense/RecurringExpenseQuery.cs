using Domain.Enums;
using Infrastructure.Repositories.Queries.Enums;

namespace Infrastructure.Repositories.Queries.RecurringExpense;

public class RecurringExpenseQuery : PaginationQuery
{
    public string Description { get; set; } = string.Empty;

    public bool? IsActive { get; set; }

    public RecurringFrequency? Frequency { get; set; }

    /// <summary>
    ///     Constrains sorting to the fields exposed by RecurringExpense.
    ///     When null, no sort is applied.
    /// </summary>
    public RecurringExpenseSortBy? SortBy { get; set; }
}