namespace RedisCache.WebApi.Services;

using RedisCache.WebApi.Models;

/// <summary>
/// Abstraction for cache operations used by business services.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default);
}
