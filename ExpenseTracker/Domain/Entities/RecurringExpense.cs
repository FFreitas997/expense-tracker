using Domain.Enums;

namespace Domain.Entities;

public class RecurringExpense : BaseEntity
{
    public required decimal Amount { get; set; }

    public required string Description { get; set; }

    public RecurringFrequency Frequency { get; set; }

    public Currency Currency { get; set; } = Currency.USD;

    public DateTime NextDueDate { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}