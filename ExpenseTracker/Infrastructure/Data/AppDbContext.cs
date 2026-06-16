using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
///     Abstraction over <see cref="AppDbContext" /> to allow mocking in unit tests.
///     Exposes only the DbSets and <see cref="SaveChangesAsync" /> that application code depends on.
/// </summary>
public interface IAppDbContext
{
    /// <summary>Gets the users table, inherited from ASP.NET Core Identity.</summary>
    DbSet<User> Users { get; }

    /// <summary>Gets the expense categories table.</summary>
    DbSet<Category> Categories { get; }

    /// <summary>Gets the expenses table.</summary>
    DbSet<Expense> Expenses { get; }

    /// <summary>Gets the budgets table.</summary>
    DbSet<Budget> Budgets { get; }

    /// <summary>Gets the recurring expenses table.</summary>
    DbSet<RecurringExpense> RecurringExpenses { get; }

    /// <summary>Persists all pending changes to the underlying database.</summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
///     Entity Framework Core database context for the Expense Tracker application.
///     Extends <see cref="IdentityDbContext{TUser,TRole,TKey}" /> to include ASP.NET Core Identity tables
///     alongside the application-specific entities.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    /// <summary>Gets or sets the expense categories table.</summary>
    public DbSet<Category> Categories { get; set; }

    /// <summary>Gets or sets the expenses table.</summary>
    public DbSet<Expense> Expenses { get; set; }

    /// <summary>Gets or sets the budgets table.</summary>
    public DbSet<Budget> Budgets { get; set; }

    /// <summary>Gets or sets the recurring expenses table.</summary>
    public DbSet<RecurringExpense> RecurringExpenses { get; set; }

    /// <summary>
    ///     Configures the entity model by applying all <see cref="IEntityTypeConfiguration{TEntity}" />
    ///     implementations found in the <see cref="Infrastructure" /> assembly.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically discover and apply all entity configurations defined in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}