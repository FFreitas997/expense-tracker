using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Table mapping
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Icon).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Color).HasMaxLength(7); // #RRGGBB
        builder.Property(c => c.IsDefault).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(c => c.ModifiedBy).HasMaxLength(256);

        // Relationships
        builder.HasOne(c => c.User)
            .WithMany(u => u.CustomCategories)
            .HasForeignKey(c => c.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Expenses)
            .WithOne(e => e.Category)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Check constraint for Color to ensure it's either null or a valid hex code
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Categories_Color_Hex", "[Color] IS NULL OR ([Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]' AND LEN([Color]) = 7)"));

        // Indexes for performance
        builder.HasIndex(c => c.UserId).HasDatabaseName("IX_Categories_UserId");
        builder.HasIndex(c => c.IsDefault).HasDatabaseName("IX_Categories_IsDefault");
        builder.HasIndex(c => c.Name).HasDatabaseName("IX_Categories_Name");
        builder.HasIndex(c => new { c.UserId, c.Name }).IsUnique().HasFilter("\"UserId\" IS NOT NULL")
            .HasDatabaseName("IX_Categories_UserId_Name_Unique");
        builder.HasIndex(c => new { c.IsDefault, c.UserId }).HasDatabaseName("IX_Categories_IsDefault_UserId");
    }
}