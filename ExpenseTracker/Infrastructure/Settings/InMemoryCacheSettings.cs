using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

public class InMemoryCacheSettings
{
    // Abstract size units — each cached entry must set MemoryCacheEntryOptions.Size for this limit to take effect
    [Range(1, int.MaxValue)]
    public int CacheSizeLimit { get; set; } = 1000;

    // Percentage of entries to evict when the size limit is reached (0.0–1.0)
    [Range(0.01, 1.0)]
    public double CacheCompactionPercentage { get; set; } = 0.20;

    // How often the cache scans for expired entries
    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(2);

    // Enables cache hit/miss statistics; should be disabled in production due to performance overhead
    public bool TrackStatistics { get; set; } = false;
}
