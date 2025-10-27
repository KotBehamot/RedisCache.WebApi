using RedisCache.WebApi.Models;
using RedisCache.WebApi.Repositories;
using StrongTypedCache.Abstractions;

namespace RedisCache.WebApi.Services;

/// <summary>
/// Product service implementing 2-level cache-aside:
/// L1: StrongTyped in-memory cache, L2: Redis via ICacheService, with graceful fallbacks.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductService> _logger;
    private readonly ICache<ProductKey, Product> _l1ProductCache;
    private readonly ICache<AllProductsKey, AllProductsCacheEntry> _l1AllProductsCache;

    private static string AllProductsKeyString => "products:all";
    private static string ProductKeyString(ProductId id) => $"products:{id.Value}";

    public ProductService(
        IProductRepository repository,
        ICacheService cache,
        ILogger<ProductService> logger,
        ICache<ProductKey, Product> l1ProductCache,
        ICache<AllProductsKey, AllProductsCacheEntry> l1AllProductsCache)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
        _l1ProductCache = l1ProductCache;
        _l1AllProductsCache = l1AllProductsCache;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default)
    {
        // L1
        if (_l1AllProductsCache.TryGetValue(new AllProductsKey(), out var cachedAll) && cachedAll is not null && cachedAll.Items is not null)
        {
            return cachedAll.Items;
        }

        // L2
        var cached = await _cache.GetAsync<IEnumerable<Product>>(AllProductsKeyString, ct);
        if (cached != null)
        {
            _l1AllProductsCache.CreateEntry(new AllProductsKey(), new AllProductsCacheEntry { Items = cached.ToList() });
            return cached;
        }

        // Source of truth
        var data = await _repository.GetAllAsync(ct);
        var list = data.ToList();

        // Populate caches
        await _cache.SetAsync(AllProductsKeyString, list, ttl: null, ct);
        _l1AllProductsCache.CreateEntry(new AllProductsKey(), new AllProductsCacheEntry { Items = list });

        return list;
    }

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
    {
        var typedKey = new ProductKey(id);

        // L1
        if (_l1ProductCache.TryGetValue(typedKey, out var l1Value) && l1Value is not null)
        {
            return l1Value;
        }

        // L2
        var key = ProductKeyString(id);
        var cached = await _cache.GetAsync<Product>(key, ct);
        if (cached != null)
        {
            _l1ProductCache.CreateEntry(typedKey, cached);
            return cached;
        }

        // Source of truth
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            await _cache.SetAsync(key, entity, ttl: null, ct);
            _l1ProductCache.CreateEntry(typedKey, entity);
        }
        return entity;
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        var created = await _repository.AddAsync(product, ct);
        // Invalidate caches
        await _cache.RemoveAsync(AllProductsKeyString, ct);
        await _cache.RemoveAsync(ProductKeyString(created.Id), ct);
        _l1AllProductsCache.Remove(new AllProductsKey());
        _l1ProductCache.Remove(new ProductKey(created.Id));
        return created;
    }

    public async Task<bool> UpdateAsync(ProductId id, Product product, CancellationToken ct = default)
    {
        product.Id = id;
        var ok = await _repository.UpdateAsync(product, ct);
        if (ok)
        {
            await _cache.RemoveAsync(AllProductsKeyString, ct);
            await _cache.RemoveAsync(ProductKeyString(id), ct);
            _l1AllProductsCache.Remove(new AllProductsKey());
            _l1ProductCache.Remove(new ProductKey(id));
        }
        return ok;
    }

    public async Task<bool> DeleteAsync(ProductId id, CancellationToken ct = default)
    {
        var ok = await _repository.DeleteAsync(id, ct);
        if (ok)
        {
            await _cache.RemoveAsync(AllProductsKeyString, ct);
            await _cache.RemoveAsync(ProductKeyString(id), ct);
            _l1AllProductsCache.Remove(new AllProductsKey());
            _l1ProductCache.Remove(new ProductKey(id));
        }
        return ok;
    }
}
