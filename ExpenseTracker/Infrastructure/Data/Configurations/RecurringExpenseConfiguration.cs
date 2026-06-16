using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class RecurringExpenseConfiguration : IEntityTypeConfiguration<RecurringExpense>
{
    public void Configure(EntityTypeBuilder<RecurringExpense> builder)
    {
        // Table mapping
        builder.ToTable("RecurringExpenses");
        builder.HasKey(r => r.Id);

        // Property configurations
        builder.Property(r => r.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(500);
        builder.Property(r => r.Currency).IsRequired().HasDefaultValue(Currency.USD);
        builder.Property(r => r.Frequency).IsRequired();
        builder.Property(r => r.NextDueDate).IsRequired();
        builder.Property(r => r.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(r => r.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(r => r.ModifiedBy).HasMaxLength(256);

        // Relationships
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Check constraint to ensure Amount is positive
        builder.ToTable(t => t.HasCheckConstraint("CK_RecurringExpenses_Amount_Positive", "\"Amount\" > 0"));

        // Indexes for performance
        builder.HasIndex(r => r.UserId).HasDatabaseName("IX_RecurringExpenses_UserId");
        builder.HasIndex(r => r.CategoryId).HasDatabaseName("IX_RecurringExpenses_CategoryId");
        builder.HasIndex(r => r.Currency).HasDatabaseName("IX_RecurringExpenses_Currency");
        builder.HasIndex(r => r.IsActive).HasDatabaseName("IX_RecurringExpenses_IsActive");
        builder.HasIndex(r => r.NextDueDate).HasDatabaseName("IX_RecurringExpenses_NextDueDate");
        builder.HasIndex(r => r.Frequency).HasDatabaseName("IX_RecurringExpenses_Frequency");
        builder.HasIndex(r => new { r.UserId, r.IsActive }).HasDatabaseName("IX_RecurringExpenses_UserId_IsActive");
        builder.HasIndex(r => new { r.IsActive, r.NextDueDate })
            .HasDatabaseName("IX_RecurringExpenses_IsActive_NextDueDate");
    }
}