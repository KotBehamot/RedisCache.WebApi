using RedisCache.WebApi.Models;

namespace RedisCache.WebApi.Repositories;

/// <summary>
/// Abstraction for product repository.
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task<bool> UpdateAsync(Product product, CancellationToken ct = default);
    Task<bool> DeleteAsync(ProductId id, CancellationToken ct = default);
}
