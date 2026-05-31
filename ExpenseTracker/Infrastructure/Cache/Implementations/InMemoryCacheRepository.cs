using System.Collections.Concurrent;
using Infrastructure.Cache.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Cache.Implementations;

/// <summary>
/// <see cref="ICacheRepository"/> implementation backed by <see cref="IMemoryCache"/>.
/// Uses a per-key <see cref="SemaphoreSlim"/> to implement a cache-aside pattern with
/// double-checked locking, ensuring the factory delegate is invoked at most once per key
/// even under concurrent access.
/// </summary>
/// <param name="cache">The underlying in-process memory cache.</param>
/// <param name="logger">Logger used to emit cache hit/miss/eviction diagnostics.</param>
/// <param name="settings">Expiration settings bound from configuration via <see cref="InMemoryCacheSettings"/>.</param>
public class InMemoryCacheRepository(
    IMemoryCache cache,
    ILogger<InMemoryCacheRepository> logger,
    IOptions<InMemoryCacheSettings> settings
) : ICacheRepository
{
    // One semaphore per key — intentionally never disposed so that waiting threads are never
    // handed a disposed SemaphoreSlim instance if eviction races with a concurrent read.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    // Resolved once at construction time to avoid repeated Options indirection on every call.
    private readonly InMemoryCacheSettings _settings = settings.Value;

    /// <summary>
    /// Returns the cached value for <paramref name="key"/> if present; otherwise invokes
    /// <paramref name="factory"/> to produce the value, stores it in the cache, and returns it.
    /// </summary>
    /// <remarks>
    /// Uses a per-key <see cref="SemaphoreSlim"/> with double-checked locking so that only one
    /// caller executes the factory for the same key at a time, preventing cache stampedes.
    /// <see langword="null"/> values returned by the factory are not cached.
    /// </remarks>
    /// <typeparam name="T">The type of the cached value. Must be a reference type.</typeparam>
    /// <param name="key">The unique cache key.</param>
    /// <param name="factory">
    /// Async delegate invoked on a cache miss to produce the value to cache.
    /// Receives a <see cref="CancellationToken"/> forwarded from the caller.
    /// </param>
    /// <param name="ct">Token used to cancel the semaphore wait or the factory invocation.</param>
    /// <returns>The cached or freshly produced value, or <see langword="null"/> if the factory returned <see langword="null"/>.</returns>
    public async Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct = default) where T : class
    {
        // Fast path: return immediately if the entry is already in the cache.
        if (cache.TryGetValue(key, out T? cachedValue))
        {
            logger.LogDebug("Cache hit for key '{Key}'.", key);
            return cachedValue;
        }

        // Obtain (or create) the semaphore for this specific key and wait for exclusive access.
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            // Double-check after acquiring the lock — another thread may have populated the entry
            // while this caller was waiting on the semaphore.
            if (cache.TryGetValue(key, out cachedValue))
            {
                logger.LogDebug("Cache hit for key '{Key}' after semaphore wait.", key);
                return cachedValue;
            }

            // Cache miss confirmed: invoke the factory to produce the value.
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
            // Always release the semaphore so subsequent waiters are unblocked.
            semaphore.Release();
        }
    }

    /// <summary>
    /// Evicts the cache entry identified by <paramref name="key"/>, if one exists.
    /// </summary>
    /// <param name="key">The unique cache key to remove.</param>
    /// <param name="ct">Not used for the in-memory implementation; included for interface compatibility.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        logger.LogDebug("Cache entry removed for key '{Key}'.", key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Builds a <see cref="MemoryCacheEntryOptions"/> instance using the expiration values
    /// from <see cref="InMemoryCacheSettings"/>.
    /// </summary>
    /// <returns>A configured <see cref="MemoryCacheEntryOptions"/> ready to be passed to <see cref="IMemoryCache.Set"/>.</returns>
    private MemoryCacheEntryOptions CreateCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            // Each entry counts as one unit toward the cache's configured size limit.
            Size = 1,
            AbsoluteExpirationRelativeToNow = _settings.AbsoluteExpiration,
            SlidingExpiration = _settings.SlidingExpiration
        };
    }
}