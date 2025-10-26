using Microsoft.EntityFrameworkCore;
using RedisCache.WebApi.Data;
using RedisCache.WebApi.Models;

namespace RedisCache.WebApi.Repositories;

/// <summary>
/// EF Core implementation of the product repository.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _db;

    public ProductRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        if (product.Id == Guid.Empty) product.Id = Guid.NewGuid();
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken ct = default)
    {
        var existing = await _db.Products.FirstOrDefaultAsync(p => p.Id == product.Id, ct);
        if (existing == null) return false;
        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.Category = product.Category;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (existing == null) return false;
        _db.Products.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
