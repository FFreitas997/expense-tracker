namespace Domain.Entities;

public class Category : BaseEntity
{
    public required string Name { get; set; }

    public required string Icon { get; set; }

    public string? Color { get; set; }

    public bool IsDefault { get; set; } = false;

    // Navigation property
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}