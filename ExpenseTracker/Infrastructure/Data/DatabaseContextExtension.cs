using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

internal static class DatabaseContextExtension
{
    internal static IServiceCollection AddDatabaseContext(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var settings = configuration.GetSection("Database").Get<DatabaseSettings>();

        if (settings is null)
            throw new InvalidOperationException("Database settings are not configured.");

        ValidateConnectionString(settings.ConnectionString);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(settings.ConnectionString, sqlOptions =>
            {
                // Set the command timeout for all SQL commands executed by this context
                sqlOptions.CommandTimeout(settings.CommandTimeout);

                // Use split queries for related collections to improve performance and reduce memory usage
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

                // Retry on transient failures (e.g., network blips, SQL Azure throttling)
                sqlOptions.EnableRetryOnFailure(settings.MaxRetryCount);
            });

            // Enable detailed errors and sensitive data logging in development for easier debugging
            options.EnableDetailedErrors(settings.EnableDetailedErrors);

            // WARNING: Enabling sensitive data logging can expose sensitive information in logs. Use with caution and only in development environments.
            options.EnableSensitiveDataLogging(settings.EnableSensitiveDataLogging);
        });

        return services;
    }

    private static void ValidateConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string is not configured.");

        // Basic validation to check if the connection string contains expected keywords for a SQL Server connection string
        if (!connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
            !connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Connection string is not a valid SQL Server connection string.");
    }
}