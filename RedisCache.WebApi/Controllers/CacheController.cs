using Microsoft.AspNetCore.Mvc;
using RedisCache.WebApi.Models;
using RedisCache.WebApi.Services;

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

    public CacheController(ILogger<CacheController> logger, ICacheService cache)
    {
        _logger = logger;
        _cache = cache;
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
    /// Clear product cache entries.
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        await _cache.RemoveByPrefixAsync("products");
        await _cache.RemoveAsync("products:all");
        return NoContent();
    }
}
