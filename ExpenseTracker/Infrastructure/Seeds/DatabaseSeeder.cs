using Infrastructure.Seeds.Seeders;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Seeds;

public class DatabaseSeeder(
    ILogger<DatabaseSeeder> logger,
    RoleSeeder roleSeeder,
    DefaultCategorySeeder categorySeeder
)
{
    public async Task SeedAsync()
    {
        logger.LogInformation("Starting database seeding...");

        await roleSeeder.SeedAsync();
        await categorySeeder.SeedAsync();

        logger.LogInformation("Database seeding completed.");
    }
}