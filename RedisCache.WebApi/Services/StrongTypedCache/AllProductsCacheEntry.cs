using System.Collections.Generic;
using RedisCache.WebApi.Models;

namespace RedisCache.WebApi.Services;

/// <summary>
/// Strong-typed L1 cache entry for the whole product list.
/// </summary>
public class AllProductsCacheEntry
{
    public List<Product> Items { get; set; } = new();
}