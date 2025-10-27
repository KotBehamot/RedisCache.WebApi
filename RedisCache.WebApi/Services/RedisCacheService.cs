using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using RedisCache.WebApi.Models;

namespace RedisCache.WebApi.Services;

/// <summary>
/// Redis-backed cache service implementing cache-aside with graceful fallback.
/// </summary>
public class RedisCacheService : ICacheService, IDisposable
{
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _keyPrefix;
    private readonly TimeSpan _defaultTtl;
    private readonly Lazy<ConnectionMultiplexer> _connectionLazy;

    public RedisCacheService(IConfiguration configuration, ILogger<RedisCacheService> logger)
    {
        _logger = logger;
        _keyPrefix = configuration["Cache:KeyPrefix"] ?? "rcw:";
        var ttlSeconds = int.TryParse(configuration["Cache:DefaultTtlSeconds"], out var s) ? s : 300;
        _defaultTtl = TimeSpan.FromSeconds(ttlSeconds);
        var connStr = configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false";
        _connectionLazy = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connStr));
    }

    private IDatabase? TryGetDb()
    {
        try
        {
            if (_connectionLazy.Value.IsConnected) return _connectionLazy.Value.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis connection not available.");
        }
        return null;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var db = TryGetDb();
        if (db == null) return default;
        try
        {
            var value = await db.StringGetAsync(_keyPrefix + key);
            if (value.HasValue)
            {
                return JsonSerializer.Deserialize<T>(value!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for {Key}", key);
        }
        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var db = TryGetDb();
        if (db == null) return;
        try
        {
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(_keyPrefix + key, json, ttl ?? _defaultTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var db = TryGetDb();
        if (db == null) return;
        try
        {
            await db.KeyDeleteAsync(_keyPrefix + key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis DEL failed for {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        try
        {
            var mux = _connectionLazy.Value;
            if (!mux.IsConnected) return;
            var endpoints = mux.GetEndPoints();
            foreach (var ep in endpoints)
            {
                var server = mux.GetServer(ep);
                if (!server.IsConnected) continue;
                var pattern = _keyPrefix + prefix + "*";
                foreach (var key in server.Keys(pattern: pattern))
                {
                    await mux.GetDatabase().KeyDeleteAsync(key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis prefix clear failed for {Prefix}", prefix);
        }
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            return Task.FromResult(_connectionLazy.Value.IsConnected);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var stats = new CacheStatistics();
        try
        {
            var mux = _connectionLazy.Value;
            if (!mux.IsConnected) return stats;
            var ep = mux.GetEndPoints().FirstOrDefault();
            if (ep == null) return stats;
            var server = mux.GetServer(ep);
            var info = await server.InfoAsync();
            var keyspace = info.FirstOrDefault(s => s.Key == "Keyspace")?.ToDictionary() ?? new();
            // keyspace like db0:keys=10,expires=0,avg_ttl=0
            if (keyspace.TryGetValue("db0", out var db0))
            {
                var parts = db0.Split(',');
                foreach (var p in parts)
                {
                    var kv = p.Split('=');
                    if (kv.Length == 2 && kv[0] == "keys" && long.TryParse(kv[1], out var k)) stats.Keys = k;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to gather Redis stats");
        }
        return stats;
    }

    public void Dispose()
    {
        if (_connectionLazy.IsValueCreated)
        {
            _connectionLazy.Value.Dispose();
        }
    }
}
