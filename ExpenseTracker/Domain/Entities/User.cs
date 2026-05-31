using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class User : IdentityUser<Guid>
{
    public required string FullName { get; set; }

    public required string Role { get; set; }

    public UserState State { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public required string CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }

    // Navigation properties
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
    public ICollection<Category> CustomCategories { get; set; } = [];
}