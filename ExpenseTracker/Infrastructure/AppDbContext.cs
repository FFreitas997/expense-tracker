using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

// For mocking purposes, we need to have an interface for the DbContext. This allows us to create a mock implementation of the DbContext for testing purposes.
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<RecurringExpense> RecurringExpenses { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    public DbSet<Category> Categories { get; set; }

    public DbSet<Expense> Expenses { get; set; }

    public DbSet<Budget> Budgets { get; set; }

    public DbSet<RecurringExpense> RecurringExpenses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}