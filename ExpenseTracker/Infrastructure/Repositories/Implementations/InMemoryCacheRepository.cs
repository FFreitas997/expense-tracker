using System.Collections.Concurrent;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.Implementations;

public class InMemoryCacheRepository(IMemoryCache cache, ILogger<InMemoryCacheRepository> logger) : ICacheRepository
{
    // Tracks all live cache keys to support future bulk-invalidation scenarios
    private readonly ConcurrentDictionary<string, byte> _cacheKeys = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T?> GetOrCreateAsync<T>(string key, T? value, CancellationToken ct = default) where T : class
    {
        if (cache.TryGetValue(key, out T? cachedValue))
        {
            logger.LogDebug("Cache hit for key '{Key}'.", key);
            return cachedValue;
        }

        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            // Double-check after acquiring the lock — another thread may have populated the entry
            if (cache.TryGetValue(key, out cachedValue))
            {
                logger.LogDebug("Cache hit for key '{Key}' after semaphore wait.", key);
                return cachedValue;
            }

            if (value is not null)
            {
                cache.Set(key, value, CreateDefaultCacheOptions());
                _cacheKeys.TryAdd(key, 0);

                logger.LogDebug("Cache entry created for key '{Key}'.", key);
            }

            return value;
        }
        finally
        {
            semaphore.Release();

            // Remove the semaphore once the entry is created — it is only needed to prevent stampedes during initial population
            if (_locks.TryRemove(key, out var removed))
                removed.Dispose();
        }
    }

    public void Remove(string key)
    {
        cache.Remove(key);
        _cacheKeys.TryRemove(key, out _);

        logger.LogDebug("Cache entry removed for key '{Key}'.", key);
    }

    private static MemoryCacheEntryOptions CreateDefaultCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            Size = 1,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.Normal
        };
    }
}