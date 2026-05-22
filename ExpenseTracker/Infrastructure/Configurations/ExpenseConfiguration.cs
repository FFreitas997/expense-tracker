using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        // Configure table name and primary key
        builder.ToTable("Expenses");
        builder.HasKey(e => e.Id);

        // Configure properties
        builder.Property(e => e.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Currency).IsRequired().HasDefaultValue(Currency.USD);
        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.PaymentMethod).IsRequired();
        builder.Property(e => e.ReceiptUrl).HasMaxLength(2048);
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ModifiedBy).HasMaxLength(256);

        // Configure relationships
        builder.HasOne(e => e.User)
            .WithMany(u => u.Expenses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_Expenses_Amount_Positive", "[Amount] > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_Expenses_Date_NotFuture", "[Date] <= GETUTCDATE()"));

        // Indexes for performance
        builder.HasIndex(e => e.UserId).HasDatabaseName("IX_Expenses_UserId");
        builder.HasIndex(e => e.CategoryId).HasDatabaseName("IX_Expenses_CategoryId");
        builder.HasIndex(e => e.Date).HasDatabaseName("IX_Expenses_Date");
        builder.HasIndex(e => e.Currency).HasDatabaseName("IX_Expenses_Currency");
        builder.HasIndex(e => new { e.UserId, e.Date }).HasDatabaseName("IX_Expenses_UserId_Date");
        builder.HasIndex(e => new { e.UserId, e.CategoryId }).HasDatabaseName("IX_Expenses_UserId_CategoryId");
    }
}