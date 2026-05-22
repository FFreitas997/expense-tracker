using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Seeds.Seeders;

public class DefaultCategorySeeder(AppDbContext context, ILogger<DefaultCategorySeeder> logger)
{
    private static readonly IReadOnlyList<Category> DefaultCategories =
    [
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000000"),
            Name = "Food & Dining",
            Icon = "🍽️",
            Color = "#FF6B6B",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        },
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000000"),
            Name = "Transportation",
            Icon = "🚗",
            Color = "#4ECDC4",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        },
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0003-0000-0000-000000000000"),
            Name = "Housing",
            Icon = "🏠",
            Color = "#45B7D1",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        },
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000000"),
            Name = "Healthcare",
            Icon = "🏥",
            Color = "#96CEB4",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        },
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0005-0000-0000-000000000000"),
            Name = "Entertainment",
            Icon = "🎬",
            Color = "#FFEAA7",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        },
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0006-0000-0000-000000000000"),
            Name = "Shopping",
            Icon = "🛍️",
            Color = "#DDA0DD",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        },
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0007-0000-0000-000000000000"),
            Name = "Education",
            Icon = "📚",
            Color = "#98D8C8",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        },
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0008-0000-0000-000000000000"),
            Name = "Travel",
            Icon = "✈️",
            Color = "#F7DC6F",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        },
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0009-0000-0000-000000000000"),
            Name = "Utilities",
            Icon = "💡",
            Color = "#AED6F1",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        },
        new()
        {
            Id = Guid.Parse("a1b2c3d4-0010-0000-0000-000000000000"),
            Name = "Other",
            Icon = "📦",
            Color = "#D5DBDB",
            IsDefault = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        }
    ];

    public async Task SeedAsync()
    {
        try
        {
            var existingIds = await context.Categories
                .Where(c => c.IsDefault && c.UserId == null)
                .Select(c => c.Id)
                .ToListAsync();

            var categoriesToAdd = DefaultCategories
                .Where(c => !existingIds.Contains(c.Id))
                .ToList();

            if (categoriesToAdd.Count == 0)
            {
                logger.LogInformation("Default categories already seeded. Skipping.");
                return;
            }

            await context.Categories.AddRangeAsync(categoriesToAdd);
            await context.SaveChangesAsync();

            var count = categoriesToAdd.Count;

            logger.LogInformation("Seeded {Count} default categories successfully.", count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding default categories.");
            throw;
        }
    }
}