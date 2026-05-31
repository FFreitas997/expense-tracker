using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configure table name and primary key
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        // Configure properties
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Role).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.State).HasDefaultValue(UserState.Active);
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(u => u.ModifiedBy).HasMaxLength(256);

        // Configure indexes
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");
        builder.HasIndex(u => u.State).HasDatabaseName("IX_Users_State");
        builder.HasIndex(u => u.Role).HasDatabaseName("IX_Users_Role");
        builder.HasIndex(u => u.CreatedAt).HasDatabaseName("IX_Users_CreatedAt");
        builder.HasIndex(u => new { u.State, u.Role }).HasDatabaseName("IX_Users_State_Role");
    }
}