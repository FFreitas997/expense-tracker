namespace Infrastructure.Repositories.Interfaces;

public interface ICacheRepository
{
    Task<T?> GetOrCreateAsync<T>(string key, T? value, CancellationToken ct = default) where T : class;

    void Remove(string key);
}