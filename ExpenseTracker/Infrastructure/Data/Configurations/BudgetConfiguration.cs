using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        // Configure table name and primary key
        builder.ToTable("Budgets");
        builder.HasKey(b => b.Id);

        // Configure properties
        builder.Property(b => b.LimitAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(b => b.Period).IsRequired();
        builder.Property(b => b.StartDate).IsRequired();
        builder.Property(b => b.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(b => b.ModifiedBy).HasMaxLength(256);

        // Configure relationships
        builder.HasOne(b => b.User)
            .WithMany(u => u.Budgets)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // Check constraint to ensure LimitAmount is positive
        builder.ToTable(t => t.HasCheckConstraint("CK_Budgets_LimitAmount_Positive", "[LimitAmount] > 0"));

        // Indexes for performance
        builder.HasIndex(b => b.UserId).HasDatabaseName("IX_Budgets_UserId");
        builder.HasIndex(b => b.CategoryId).HasDatabaseName("IX_Budgets_CategoryId");
        builder.HasIndex(b => b.Period).HasDatabaseName("IX_Budgets_Period");
        builder.HasIndex(b => b.StartDate).HasDatabaseName("IX_Budgets_StartDate");
        builder.HasIndex(b => new { b.UserId, b.Period }).HasDatabaseName("IX_Budgets_UserId_Period");
        builder.HasIndex(b => new { b.UserId, b.CategoryId }).HasDatabaseName("IX_Budgets_UserId_CategoryId");
    }
}