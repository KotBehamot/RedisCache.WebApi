using RedisCache.WebApi.Models;

namespace RedisCache.WebApi.Services;

/// <summary>
/// Abstraction for product business logic.
/// </summary>
public interface IProductService
{
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product> CreateAsync(Product product, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, Product product, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
