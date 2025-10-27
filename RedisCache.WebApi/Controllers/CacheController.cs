using Microsoft.AspNetCore.Mvc;
using RedisCache.WebApi.Models;
using RedisCache.WebApi.Services;
using StrongTypedCache.Abstractions;

namespace RedisCache.WebApi.Controllers;

/// <summary>
/// Cache utilities endpoints.
/// </summary>
[ApiController]
[Route("api/cache")]
public class CacheController : ControllerBase
{
    private readonly ILogger<CacheController> _logger;
    private readonly ICacheService _cache;
    private readonly ICache<AllProductsKey, AllProductsCacheEntry> _l1AllProductsCache;
    private readonly ICache<ProductKey, Product> _l1ProductCache;

    public CacheController(
        ILogger<CacheController> logger,
        ICacheService cache,
        ICache<AllProductsKey, AllProductsCacheEntry> l1AllProductsCache,
        ICache<ProductKey, Product> l1ProductCache)
    {
        _logger = logger;
        _cache = cache;
        _l1AllProductsCache = l1AllProductsCache;
        _l1ProductCache = l1ProductCache;
    }

    /// <summary>
    /// Get basic cache statistics (best-effort).
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CacheStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _cache.GetStatisticsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// L1 strong-typed cache snapshot.
    /// </summary>
    [HttpGet("l1")] 
    public IActionResult GetL1()
    {
        var allEntry = _l1AllProductsCache.GetAllValues().FirstOrDefault();
        var productEntries = _l1ProductCache.GetAllValues();
        return Ok(new
        {
            l1AllProductsCached = allEntry?.Items?.Count ?? 0,
            l1ProductEntries = productEntries.Count,
        });
    }

    /// <summary>
    /// Check if specific product is present in L1 cache.
    /// </summary>
    [HttpGet("l1/product/{id}")]
    public IActionResult GetL1Product([FromRoute] ProductId id)
    {
        var exists = _l1ProductCache.TryGetValue(new ProductKey(id), out var value) && value is not null;
        return Ok(new { exists });
    }

    /// <summary>
    /// Remove specific product from L1 cache.
    /// </summary>
    [HttpDelete("l1/product/{id}")]
    public IActionResult RemoveL1Product([FromRoute] ProductId id)
    {
        _l1ProductCache.Remove(new ProductKey(id));
        return NoContent();
    }

    /// <summary>
    /// Check if specific product is present in L2 (Redis) cache.
    /// </summary>
    [HttpGet("l2/product/{id}")]
    public async Task<IActionResult> GetL2Product([FromRoute] ProductId id, CancellationToken ct)
    {
        var exists = await _cache.GetAsync<Product>($"products:{id.Value}", ct) is not null;
        return Ok(new { exists });
    }

    /// <summary>
    /// Clear product cache entries (L1 and L2).
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        // L2 Redis
        await _cache.RemoveByPrefixAsync("products");
        await _cache.RemoveAsync("products:all");
        // L1 StrongTyped
        _l1AllProductsCache.Remove(new AllProductsKey());
        return NoContent();
    }
}
