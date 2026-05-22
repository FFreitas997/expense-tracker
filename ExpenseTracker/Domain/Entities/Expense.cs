using Domain.Enums;

namespace Domain.Entities;

public class Expense : BaseEntity
{
    public required decimal Amount { get; set; }

    public required string Description { get; set; }

    public Currency Currency { get; set; } = Currency.USD;

    public required DateTime Date { get; set; }

    public required PaymentMethod PaymentMethod { get; set; }

    public string? ReceiptUrl { get; set; }

    // Navigation property
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}