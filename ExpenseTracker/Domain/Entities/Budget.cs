using Domain.Enums;

namespace Domain.Entities;

public class Budget : BaseEntity
{
    public required decimal LimitAmount { get; set; }

    public required BudgetPeriod Period { get; set; }

    public required DateTime StartDate { get; set; }

    // Navigation property
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
}