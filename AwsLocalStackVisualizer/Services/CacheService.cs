using System.Collections.Concurrent;

namespace AwsLocalStackVisualizer.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task ClearAsync();
    Task<bool> ExistsAsync(string key);
}

public class CacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private readonly ILogger<CacheService> _logger;

    public CacheService(ILogger<CacheService> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out var item) && !item.IsExpired())
        {
            return Task.FromResult(item.Value as T);
        }

        if (item != null)
        {
            _cache.TryRemove(key, out _);
        }

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var expirationTime = expiration ?? TimeSpan.FromMinutes(5);
        var cacheItem = new CacheItem(value, DateTime.UtcNow.Add(expirationTime));
        _cache.AddOrUpdate(key, cacheItem, (_, _) => cacheItem);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        if (_cache.TryGetValue(key, out var item))
        {
            if (!item.IsExpired())
            {
                return Task.FromResult(true);
            }
            _cache.TryRemove(key, out _);
        }
        return Task.FromResult(false);
    }

    private class CacheItem
    {
        public object Value { get; }
        public DateTime ExpirationTime { get; }

        public CacheItem(object value, DateTime expirationTime)
        {
            Value = value;
            ExpirationTime = expirationTime;
        }

        public bool IsExpired() => DateTime.UtcNow > ExpirationTime;
    }
}
