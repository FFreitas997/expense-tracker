using System.Collections.Concurrent;
using Infrastructure.Repositories.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Repositories.Implementations;

public class InMemoryCacheRepository(
    IMemoryCache cache,
    ILogger<InMemoryCacheRepository> logger,
    IOptions<InMemoryCacheSettings> settings
) : ICacheRepository
{
    private readonly InMemoryCacheSettings _settings = settings.Value;

    // One semaphore per key — never disposed so waiting threads are never handed a disposed instance
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, CancellationToken ct = default) where T : class
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

            var value = await factory(ct);

            if (value is not null)
            {
                cache.Set(key, value, CreateCacheOptions());
                logger.LogDebug("Cache entry created for key '{Key}'.", key);
            }

            return value;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        logger.LogDebug("Cache entry removed for key '{Key}'.", key);
        return Task.CompletedTask;
    }

    private MemoryCacheEntryOptions CreateCacheOptions() => new()
    {
        Size = 1,
        AbsoluteExpirationRelativeToNow = _settings.AbsoluteExpiration,
        SlidingExpiration = _settings.SlidingExpiration,
    };
}
