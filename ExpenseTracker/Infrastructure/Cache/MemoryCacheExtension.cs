using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Cache;

internal static class MemoryCacheExtension
{
    internal static IServiceCollection AddInMemoryCache(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var settings = configuration.GetSection("InMemoryCache").Get<InMemoryCacheSettings>();

        if (settings is null)
            throw new InvalidOperationException("InMemoryCache settings are not configured properly.");

        services.Configure<InMemoryCacheSettings>(configuration.GetSection("InMemoryCache"));

        services.AddMemoryCache(options =>
        {
            // Abstract size units — each cached entry must set MemoryCacheEntryOptions.Size for this limit to take effect
            options.SizeLimit = settings.CacheSizeLimit;

            // Evict the configured percentage of entries when the size limit is reached
            options.CompactionPercentage = settings.CacheCompactionPercentage;

            // Scan for expired entries at the configured interval to keep memory usage bounded
            options.ExpirationScanFrequency = settings.ExpirationScanFrequency;

            // Track statistics only outside production to avoid the performance overhead in live environments
            options.TrackStatistics = settings.TrackStatistics && !environment.IsProduction();
        });

        return services;
    }
}