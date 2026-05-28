using Domain.Enums;
using Infrastructure.Repositories.Queries.Enums;

namespace Infrastructure.Repositories.Queries.Expense;

public class ExpenseQuery : PaginationQuery
{
    public string Description { get; set; } = string.Empty;

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    ///     Constrains sorting to the fields exposed by Expense.
    ///     When null, no sort is applied.
    /// </summary>
    public ExpenseSortBy? SortBy { get; set; }
}