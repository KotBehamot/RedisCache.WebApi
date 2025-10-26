using RedisCache.WebApi.Models;
using RedisCache.WebApi.Repositories;

namespace RedisCache.WebApi.Services;

/// <summary>
/// Product service implementing cache-aside with Redis and graceful fallbacks.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductService> _logger;

    private static string AllProductsKey => "products:all";
    private static string ProductKey(Guid id) => $"products:{id}";

    public ProductService(IProductRepository repository, ICacheService cache, ILogger<ProductService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<IEnumerable<Product>>(AllProductsKey, ct);
        if (cached != null) return cached;

        var data = await _repository.GetAllAsync(ct);
        await _cache.SetAsync(AllProductsKey, data, ttl: null, ct);
        return data;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var key = ProductKey(id);
        var cached = await _cache.GetAsync<Product>(key, ct);
        if (cached != null) return cached;

        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            await _cache.SetAsync(key, entity, ttl: null, ct);
        }
        return entity;
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        var created = await _repository.AddAsync(product, ct);
        // Invalidate caches
        await _cache.RemoveAsync(AllProductsKey, ct);
        await _cache.RemoveAsync(ProductKey(created.Id), ct);
        return created;
    }

    public async Task<bool> UpdateAsync(Guid id, Product product, CancellationToken ct = default)
    {
        product.Id = id;
        var ok = await _repository.UpdateAsync(product, ct);
        if (ok)
        {
            await _cache.RemoveAsync(AllProductsKey, ct);
            await _cache.RemoveAsync(ProductKey(id), ct);
        }
        return ok;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var ok = await _repository.DeleteAsync(id, ct);
        if (ok)
        {
            await _cache.RemoveAsync(AllProductsKey, ct);
            await _cache.RemoveAsync(ProductKey(id), ct);
        }
        return ok;
    }
}
