namespace RedisCache.WebApi.Models;

/// <summary>
/// Simple cache statistics model.
/// </summary>
public class CacheStatistics
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long Evictions { get; set; }
    public long Keys { get; set; }
}
